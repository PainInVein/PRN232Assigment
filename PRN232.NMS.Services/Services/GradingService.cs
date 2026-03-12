using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PRN232.NMS.Repo.DBContext;
using PRN232.NMS.Repo.Entities;
using PRN232.NMS.Services;
using PRN232.NMS.Services.Helpers.HelperClasses;
using PRN232.NMS.Services.Helpers.HelperEntities;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Grader.Services;

public class GradingService
{
    private readonly ILogger<GradingService> _logger;
    private readonly Prn232lab3Context _graderDb;
    private readonly IClassHelperFacade _helperFacade;

    public GradingService(
        ILogger<GradingService> logger,
        IConfiguration config,
        Prn232lab3Context graderDb,
        IClassHelperFacade helperFacade
        )
    {
        _logger = logger;
        _graderDb = graderDb;
        _helperFacade = helperFacade;
    }

    public async Task<GradingResultHere> GradeAsync(GradingRequest req, CancellationToken ct = default)
    {
        var result = new GradingResultHere
        {
            ProjectFolder = req.ProjectFolder,
            Logs = new List<string> { $"Started grading at {DateTimeOffset.Now:HH:mm:ss}" }
        };


        string? tempDir = null;
        string? dbName = null;
        Process? apiProcess = null;

        try
        {
            // Chỗ này copy project submission vào temp folder
            var baseTempFolder = @"C:\Users\Admin\Desktop\PRNGrading";
            tempDir = Path.Combine(baseTempFolder, $"grade-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            _helperFacade.CopyDirectory(req.ProjectFolder, tempDir);
            result.Logs.Add("Project copied to temporary folder");

            // Chỗ này tạo database tạm thời cho sinh viên
            var guidPart = Guid.NewGuid().ToString("N")[..8];
            dbName = $"Grade_{DateTime.UtcNow:yyyyMMddHHmmss}_{guidPart}";
            await _helperFacade.CreateDatabaseAsync(dbName);
            result.Logs.Add($"Database {dbName} created");

            var studentConnStr = _helperFacade.BuildStudentConnectionString(dbName);
            await _helperFacade.ApplySchemaAsync(dbName);
            result.Logs.Add("Schema applied");

            // Chỉnh sửa connection string trong project để trỏ vào database mới tạo
            await _helperFacade.PatchConnectionStringAsync(tempDir, studentConnStr);
            result.Logs.Add("Connection string updated");

            // Build project
            bool buildOk = await _helperFacade.BuildAsync(tempDir, result.Logs);
            if (!buildOk)
            {
                result.Status = "BuildFailed";
                result.Score = 0;
                return result;
            }

            // Chạy API từ temp folder, truyền logs vào để capture output
            int port = _helperFacade.GetFreePort(5100, 5200);
            string baseUrl = $"http://localhost:{port}";
            apiProcess = _helperFacade.StartApi(tempDir, port, result.Logs);

            bool started = await _helperFacade.WaitForApiReadyAsync(baseUrl, ct);
            if (!started)
            {
                result.Status = "StartupTimeout";
                return result;
            }
            result.Logs.Add($"API responding on {baseUrl}");

            // Thử nghiệm route discovery để tìm ra các endpoint thực tế trên API của sinh viên
            var routeMap = await _helperFacade.DiscoverRoutesAsync(baseUrl, result.Logs);

            // Chay qua từng test case, sử dụng routeMap để resolve đường dẫn chính xác, và tính điểm
            var outcomes = await ExecuteApiTestsAsync(baseUrl, result.Logs, routeMap);
            result.Score = outcomes.Where(o => o.Passed).Sum(o => o.Points);
            result.Logs.AddRange(outcomes.Select(o => $"{o.Name,-32} {(o.Passed ? "PASS" : "FAIL"),-6} {o.Points,3} pts  {o.Message ?? ""}"));

            result.Status = result.Score >= 5 ? "Passed" : "Failed";
        }
        catch (Exception ex)
        {
            result.Status = "Exception";
            result.Logs.Add($"CRITICAL: {ex.GetType().Name}: {ex.Message}");
            _logger.LogError(ex, "Grading crashed");
        }
        finally
        {
            // Dọn sạch database và process sau khi xong
            if (apiProcess is { HasExited: false })
            {
                try { apiProcess.Kill(true); } catch { }
                result.Logs.Add("API process terminated");
            }

            if (dbName != null) await _helperFacade.TryDropDatabaseAsync(dbName);
            if (tempDir != null && Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); }
                catch { result.Logs.Add("Warning: could not delete temp folder"); }
            }

            result.FinishedAt = DateTime.UtcNow;

            var mappedResult = new GradingResult
            {
                ProjectFolder = result.ProjectFolder,
                Status = result.Status,
                Score = result.Score,
                Logs = string.Join("\n", result.Logs),
            };


            await SaveResultAsync(mappedResult);
        }

        return result;
    }

    //private
    public async Task<List<TestOutcome>> ExecuteApiTestsAsync(string baseUrl, List<string> gradingLogs, RouteMap routes)
    {
        var tests = _helperFacade.GetTestSuite();
        var outcomes = new List<TestOutcome>();
        using var client = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(6) };

        foreach (var t in tests)
        {
            try
            {
                // Resolve path through route map (handles students who used different route names)
                string resolvedPath = routes.Resolve(t.Method, t.Path, t.PathHint);
                var msg = new HttpRequestMessage(t.Method, resolvedPath);
                if (t.JsonBody != null)
                    msg.Content = new StringContent(t.JsonBody, Encoding.UTF8, "application/json");

                // Attach bearer token when credentials are provided
                if (!string.IsNullOrEmpty(t.BearerTokenEmail))
                {
                    var token = await _helperFacade.FetchTokenAsync(baseUrl, t.BearerTokenEmail, t.BearerTokenPassword ?? "@1", gradingLogs, routes);
                    if (token != null)
                        msg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    else
                        gradingLogs.Add($"[WARN] No token for {t.BearerTokenEmail} — \"{t.Name}\" will run without auth (path: {resolvedPath})");
                }



                var resp = await client.SendAsync(msg);

                string? body = null;
                if (t.ExpectedContentContains != null || !resp.IsSuccessStatusCode)
                    body = await resp.Content.ReadAsStringAsync();

                bool contentOk = t.ExpectedContentContains == null ||
                                 (body?.Contains(t.ExpectedContentContains, StringComparison.OrdinalIgnoreCase) ?? false);

                // Accept alternate status codes (200 OR 201, 200 OR 204, etc.)
                int actual = (int)resp.StatusCode;
                bool statusOk = actual == t.ExpectedStatus ||
                                (t.AlternateStatus.HasValue && actual == t.AlternateStatus.Value);

                bool pass = statusOk && contentOk;
                int bodyLen = body?.Length ?? 0;

                outcomes.Add(new TestOutcome
                {
                    Name = t.Name,
                    Passed = pass,
                    Points = pass ? t.Points : 0,
                    Message = pass ? null : $"Status={actual}, expected {t.ExpectedStatus}{(t.AlternateStatus.HasValue ? $" or {t.AlternateStatus}" : "")}. Body: {body?[..Math.Min(150, bodyLen)]}..."
                });
            }
            catch (Exception ex)
            {
                outcomes.Add(new TestOutcome
                {
                    Name = t.Name,
                    Passed = false,
                    Points = 0,
                    Message = ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ? "Timeout" : $"Exception: {ex.Message}"
                });
            }
        }
        return outcomes;
    }

    public class RouteMap
    {
        // key: (METHOD, path_hint_keyword)  value: actual path on this student's API
        private readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase);
        public string? AuthPath { get; set; }
        public string? ProfileCollectionPath { get; set; }  // the /api/XxxProfile  path
        public string? ProfileItemPath { get; set; }        // the /api/XxxProfile/{id} path
        public List<string> AllPaths { get; } = new();

        public void Add(string method, string hint, string actualPath) =>
            _map[$"{method.ToUpper()}:{hint.ToLower()}"] = actualPath;

        /// <summary>
        /// Given an expected path like "/api/LeopardProfile" or "/api/LeopardProfile/1",
        /// return the discovered equivalent on this student's API.
        /// Falls back to the original path if nothing found.
        /// </summary>
        public string Resolve(HttpMethod method, string expectedPath, string? hint = null)
        {
            // ── OData hint: find /search sub-path or fall back to collection ──
            if (hint == "odata")
            {
                var odataQuery = expectedPath.Contains('?') ? expectedPath.Substring(expectedPath.IndexOf('?')) : "";
                var searchPath = AllPaths.FirstOrDefault(p =>
                    p.EndsWith("/search", StringComparison.OrdinalIgnoreCase) &&
                    (p.Contains("leopard", StringComparison.OrdinalIgnoreCase) || p.Contains("profile", StringComparison.OrdinalIgnoreCase)));
                if (searchPath != null) return searchPath + odataQuery;
                if (ProfileCollectionPath != null) return ProfileCollectionPath + odataQuery;
            }

            // ── DELETE hint: handle students who use query param ?id=N instead of route /{id} ──
            if (hint != null && hint.StartsWith("delete_") && int.TryParse(hint.Substring(7), out int deleteId))
            {
                if (ProfileItemPath != null)
                    return ProfileItemPath.Replace("{id}", deleteId.ToString(), StringComparison.OrdinalIgnoreCase)
                                         .Replace("{Id}", deleteId.ToString());
                if (ProfileCollectionPath != null)
                    return $"{ProfileCollectionPath}?id={deleteId}";
            }

            // ── Explicit map hint ──────────────────────────────────────────────
            if (hint != null && _map.TryGetValue($"{method.Method.ToUpper()}:{hint.ToLower()}", out var byHint))
                return byHint;

            // ── Direct match ───────────────────────────────────────────────────
            if (AllPaths.Contains(expectedPath, StringComparer.OrdinalIgnoreCase))
                return expectedPath;

            // ── Pattern matching ───────────────────────────────────────────────
            var lower = expectedPath.ToLower();

            if (lower.Contains("leopardprofile") || lower.Contains("leopard"))
            {
                var parts = expectedPath.Trim('/').Split('/');
                bool hasId = parts.Length > 0 && (int.TryParse(parts[^1], out _) || parts[^1].StartsWith("{"));
                bool hasOdata = expectedPath.Contains('?');

                if (hasOdata && ProfileCollectionPath != null)
                    return ProfileCollectionPath + expectedPath.Substring(expectedPath.IndexOf('?'));
                if (hasId && ProfileItemPath != null)
                    return ProfileItemPath.Replace("{id}", parts[^1], StringComparison.OrdinalIgnoreCase)
                                         .Replace("{Id}", parts[^1]);
                if (!hasId && ProfileCollectionPath != null)
                    return ProfileCollectionPath;
            }

            if ((lower.Contains("auth") || lower.Contains("login")) && method == HttpMethod.Post && AuthPath != null)
                return AuthPath;

            return expectedPath; // fallback — use original
        }
    }

    //private
    public async Task SaveResultAsync(GradingResult r)
    {
        // Map to your EF entity
        var entity = new GradingResult
        {
            StudentId = r.StudentId,
            ProjectFolder = r.ProjectFolder,
            Status = r.Status,
            Score = r.Score,
            Logs = string.Join("\n", r.Logs),
        };

        _graderDb.GradingResults.Add(entity);
        await _graderDb.SaveChangesAsync();
    }

    public class GradingResultHere
    {
        public string ProjectFolder { get; set; } = null!;
        public string Status { get; set; } = null!;
        public int Score { get; set; }
        public List<string> Logs { get; set; } = new();
        public DateTime FinishedAt { get; set; }
    }
}