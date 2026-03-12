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
    private readonly string _masterConnStr;
    private readonly string _schemaPath;
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
        _masterConnStr = "Server=DESKTOP-H9I435N\\SQLEXPRESS;User Id=sa;Password=1;TrustServerCertificate=true;";
        _schemaPath = @"C:\Users\Admin\Desktop\PRNGrading\SU25LeopardDB.sql";
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
            _logger.LogError(ex, "Grading crashed for {StudentId}");
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

    // ────────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────────────────────

    //private static - done
    public void CopyDirectory(string source, string target)
    {
        var excludedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".vs", "bin", "obj", ".git", "node_modules"
    };

        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(dir);

            if (excludedDirs.Contains(name))
                continue;

            var relative = Path.GetRelativePath(source, dir);
            Directory.CreateDirectory(Path.Combine(target, relative));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);

            if (relative.Split(Path.DirectorySeparatorChar).Any(x => excludedDirs.Contains(x)))
                continue;

            var dest = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, true);
        }
    }

    //private - done
    public async Task CreateDatabaseAsync(string dbName)
    {
        using var conn = new SqlConnection(_masterConnStr);
        await conn.OpenAsync();
        using var cmd = new SqlCommand($"CREATE DATABASE [{dbName}]", conn);
        await cmd.ExecuteNonQueryAsync();
    }

    //private - done
    public async Task TryDropDatabaseAsync(string dbName)
    {
        try
        {
            using var conn = new SqlConnection(_masterConnStr);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(
                $"""
                ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{dbName}];
                """, conn);
            await cmd.ExecuteNonQueryAsync();
        }
        catch { /* best effort */ }
    }

    //private - done
    public async Task ApplySchemaAsync(string dbName)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "sqlcmd",
            Arguments = $@"-S DESKTOP-H9I435N\SQLEXPRESS -U sa -P 1 -d {dbName} -i ""{_schemaPath}""",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi)!;
        string err = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();

        if (p.ExitCode != 0)
            throw new Exception($"sqlcmd failed (exit {p.ExitCode}): {err}");
    }

    //private - done
    public string BuildStudentConnectionString(string dbName) =>
        $"Data Source=DESKTOP-H9I435N\\SQLEXPRESS;Initial Catalog={dbName};User ID=sa;Password=1;TrustServerCertificate=true;";

    //private - done
    public async Task PatchConnectionStringAsync(string projectDir, string connStr)
    {
        // ── Patch every appsettings*.json found in project ────────────────────
        var appSettingsFiles = Directory.GetFiles(projectDir, "appsettings*.json", SearchOption.AllDirectories);
        foreach (var path in appSettingsFiles)
        {
            try
            {
                var json = await File.ReadAllTextAsync(path);
                if (!json.Contains("ConnectionStrings", StringComparison.OrdinalIgnoreCase)) continue;

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement.Clone();
                var writerOptions = new JsonWriterOptions { Indented = true };
                using var ms = new MemoryStream();
                using var writer = new Utf8JsonWriter(ms, writerOptions);

                writer.WriteStartObject();
                bool wroteConnStr = false;
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.NameEquals("ConnectionStrings"))
                    {
                        writer.WritePropertyName("ConnectionStrings");
                        writer.WriteStartObject();
                        // Preserve whatever key names the student used (DBConnection, DefaultConnection, etc.)
                        bool wroteSomething = false;
                        foreach (var cs in prop.Value.EnumerateObject())
                        {
                            writer.WriteString(cs.Name, connStr);
                            wroteSomething = true;
                        }
                        if (!wroteSomething)
                            writer.WriteString("DefaultConnection", connStr);
                        writer.WriteEndObject();
                        wroteConnStr = true;
                    }
                    else
                    {
                        prop.WriteTo(writer);
                    }
                }
                if (!wroteConnStr)
                {
                    // No ConnectionStrings section existed — add one
                    writer.WritePropertyName("ConnectionStrings");
                    writer.WriteStartObject();
                    writer.WriteString("DefaultConnection", connStr);
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
                writer.Flush();
                await File.WriteAllBytesAsync(path, ms.ToArray());
            }
            catch { /* best effort per file */ }
        }

        // ── Patch hardcoded OnConfiguring() in *Context.cs files ─────────────
        // Some students scaffold with a hardcoded connection string in OnConfiguring().
        // We regex-replace the UseSqlServer("...") argument with the grader's connStr.
        await PatchHardcodedDbContextAsync(projectDir, connStr);
    }

    //private static - done
    public async Task PatchHardcodedDbContextAsync(string projectDir, string connStr)
    {
        var csFiles = Directory.GetFiles(projectDir, "*Context.cs", SearchOption.AllDirectories)
                               .Concat(Directory.GetFiles(projectDir, "*DBContext.cs", SearchOption.AllDirectories))
                               .Distinct();

        foreach (var file in csFiles)
        {
            try
            {
                var src = await File.ReadAllTextAsync(file);

                if (!src.Contains("OnConfiguring", StringComparison.OrdinalIgnoreCase)) continue;
                if (!src.Contains("UseSqlServer", StringComparison.OrdinalIgnoreCase)) continue;

                // Escape characters for C# string literal
                var escaped = connStr.Replace("\\", "\\\\").Replace("\"", "\\\"");

                // Replace double quoted argument
                var patched = System.Text.RegularExpressions.Regex.Replace(
                    src,
                    @"optionsBuilder\.UseSqlServer\(""[^""]*""\)",
                    $"optionsBuilder.UseSqlServer(\"{escaped}\")"
                );

                // Replace verbatim string argument @"..."
                patched = System.Text.RegularExpressions.Regex.Replace(
                    patched,
                    @"optionsBuilder\.UseSqlServer\(@""[^""]*""\)",
                    $"optionsBuilder.UseSqlServer(\"{escaped}\")"
                );

                if (patched != src)
                    await File.WriteAllTextAsync(file, patched);
            }
            catch
            {
                /* best effort */
            }
        }
    }

    //private - done
    public async Task<bool> BuildAsync(string dir, List<string> logs)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build",
            WorkingDirectory = dir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi)!;
        string output = await p.StandardOutput.ReadToEndAsync();
        string err = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();

        logs.Add("Build Succeeded");

        return p.ExitCode == 0;
    }

    //private - done
    public Process StartApi(string dir, int port, List<string> logs)
    {
        var apiProject = Directory
            .GetFiles(dir, "*.csproj", SearchOption.AllDirectories)
            .First(x => File.ReadAllText(x).Contains("Microsoft.NET.Sdk.Web"));

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --no-build --no-launch-profile --project \"{apiProject}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.Environment["ASPNETCORE_URLS"] = $"http://localhost:{port}";
        psi.Environment["ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"] = "";

        var p = Process.Start(psi)!;

        _ = Task.Run(async () =>
        {
            string? line;
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
                logs.Add($"[API OUT] {line}");

            while ((line = await p.StandardError.ReadLineAsync()) != null)
                logs.Add($"[API ERR] {line}");
        });

        return p;
    }

    //private static
    public readonly string[] _readinessProbePaths =
        { "/api/LeopardProfile", "/api/Leopard", "/swagger/v1/swagger.json", "/" };

    //private
    public async Task<bool> WaitForApiReadyAsync(string baseUrl, CancellationToken ct, int timeoutSec = 30)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        var sw = Stopwatch.StartNew();

        while (sw.Elapsed.TotalSeconds < timeoutSec)
        {
            if (ct.IsCancellationRequested) return false;

            foreach (var probe in _readinessProbePaths)
            {
                try
                {
                    var resp = await http.GetAsync(baseUrl + probe, ct);
                    // Any non-5xx response means the server is up and listening.
                    if ((int)resp.StatusCode < 500) return true;
                }
                catch { }
            }

            await Task.Delay(800, ct);
        }
        return false;
    }

    // ── Per-run token cache ───────────────────────────────────────────────────
    //private
    public readonly Dictionary<string, string?> _tokenCache = new();

    // Try multiple common auth endpoint paths students might use
    //private static
    public readonly string[] _authPaths = { "/api/auth", "/api/login", "/api/account/login", "/api/accounts/login", "/api/authenticate" };

    //private static - done
    public string? FindTokenInElement(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;
        foreach (var prop in element.EnumerateObject())
        {
            // Accept common token field names
            if (prop.Value.ValueKind == JsonValueKind.String &&
                (prop.Name.Equals("token", StringComparison.OrdinalIgnoreCase) ||
                 prop.Name.Equals("accessToken", StringComparison.OrdinalIgnoreCase) ||
                 prop.Name.Equals("access_token", StringComparison.OrdinalIgnoreCase) ||
                 prop.Name.Equals("jwt", StringComparison.OrdinalIgnoreCase)))
                return prop.Value.GetString();
            // Recurse into nested objects (e.g. { "data": { "token": "..." } })
            if (prop.Value.ValueKind == JsonValueKind.Object)
            {
                var nested = FindTokenInElement(prop.Value);
                if (nested != null) return nested;
            }
        }
        return null;
    }

    //private - done
    public async Task<string?> FetchTokenAsync(string baseUrl, string email, string password, List<string> logs, RouteMap? routes = null)
    {
        if (_tokenCache.TryGetValue(email, out var cached))
        {
            logs.Add($"[AUTH] Cache hit for {email}: token {(cached != null ? "present" : "NULL — login previously failed")}");
            return cached;
        }

        using var client = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(6) };
        var bodyJson = $"{{\"email\":\"{email}\",\"password\":\"{password}\"}}";

        // Build path list: discovered path first, then fallbacks
        var authPathsToTry = new List<string>();
        if (routes?.AuthPath != null) authPathsToTry.Add(routes.AuthPath);
        foreach (var p in _authPaths) if (!authPathsToTry.Contains(p)) authPathsToTry.Add(p);

        foreach (var path in authPathsToTry)
        {
            try
            {
                var msg = new HttpRequestMessage(HttpMethod.Post, path);
                msg.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                var resp = await client.SendAsync(msg);
                var respBody = await resp.Content.ReadAsStringAsync();
                logs.Add($"[AUTH] POST {path} for {email} → {(int)resp.StatusCode}  body: {respBody[..Math.Min(200, respBody.Length)]}");

                if (!resp.IsSuccessStatusCode) continue;

                var token = FindTokenInElement(JsonDocument.Parse(respBody).RootElement);
                if (token != null)
                {
                    logs.Add($"[AUTH] Token obtained for {email} via {path}");
                    _tokenCache[email] = token;
                    return token;
                }

                logs.Add($"[AUTH] Login at {path} returned 200 but no token field found. Raw: {respBody[..Math.Min(300, respBody.Length)]}");
                _tokenCache[email] = null;
                return null; // found the auth endpoint, token extraction failed — stop trying
            }
            catch (Exception ex)
            {
                logs.Add($"[AUTH] {path} exception: {ex.Message}");
            }
        }

        logs.Add($"[AUTH] All auth paths failed for {email}");
        _tokenCache[email] = null;
        return null;
    }

    //private
    public async Task<List<TestOutcome>> ExecuteApiTestsAsync(string baseUrl, List<string> gradingLogs, RouteMap routes)
    {
        _tokenCache.Clear();
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

    // ────────────────────────────────────────────────────────────────────────────
    //  Route Discovery — reads /swagger/v1/swagger.json to find actual API paths
    // ────────────────────────────────────────────────────────────────────────────

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

    //private - done
    public async Task<RouteMap> DiscoverRoutesAsync(string baseUrl, List<string> logs)
    {
        var map = new RouteMap();
        using var client = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(5) };

        // Try Swagger JSON
        foreach (var swaggerPath in new[] { "/swagger/v1/swagger.json", "/swagger/v1.0/swagger.json", "/openapi/v1.json" })
        {
            try
            {
                var resp = await client.GetAsync(swaggerPath);
                if (!resp.IsSuccessStatusCode) continue;

                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("paths", out var paths)) continue;

                foreach (var pathProp in paths.EnumerateObject())
                {
                    var rawPath = pathProp.Name; // e.g. "/api/LeopardProfile" or "/api/Auth"
                    map.AllPaths.Add(rawPath);
                    var lower = rawPath.ToLower();

                    foreach (var methodProp in pathProp.Value.EnumerateObject())
                    {
                        var method = methodProp.Name.ToUpper();

                        // Auth endpoint: POST with no {id} in path, path contains auth/login/account
                        if (method == "POST" && (lower.Contains("auth") || lower.Contains("login")) && !lower.Contains("{"))
                        {
                            map.AuthPath = rawPath;
                            logs.Add($"[ROUTES] Auth endpoint discovered: POST {rawPath}");
                        }

                        // Profile collection: path contains leopard or profile, no {id}
                        if (!lower.Contains("{") && (lower.Contains("leopard") || lower.Contains("profile")))
                        {
                            if (map.ProfileCollectionPath == null || rawPath.Length < map.ProfileCollectionPath.Length)
                            {
                                map.ProfileCollectionPath = rawPath;
                                logs.Add($"[ROUTES] Profile collection endpoint: {method} {rawPath}");
                            }
                        }

                        // Profile item: same but has {id}
                        if (lower.Contains("{") && (lower.Contains("leopard") || lower.Contains("profile")))
                        {
                            map.ProfileItemPath = rawPath;
                            logs.Add($"[ROUTES] Profile item endpoint: {method} {rawPath}");
                        }
                    }
                }

                logs.Add($"[ROUTES] Discovery complete. Auth={map.AuthPath}, Collection={map.ProfileCollectionPath}, Item={map.ProfileItemPath}");
                logs.Add($"[ROUTES] All paths: {string.Join(", ", map.AllPaths)}");
                return map;
            }
            catch (Exception ex)
            {
                logs.Add($"[ROUTES] {swaggerPath} failed: {ex.Message}");
            }
        }

        logs.Add("[ROUTES] Swagger not available — falling back to hardcoded paths");
        return map;
    }

    //private - done
    public List<ApiTestCase> GetTestSuite() => new()
{
    // ── 1. Authentication  (~1.54 pts total) ──────────────────────────

    new()
    {
        Name = "Login success (administrator) → 200 + token",
        Method = HttpMethod.Post,
        Path = "/api/auth",
        JsonBody = """{"email":"administrator@leopard.com","password":"@1"}""",
        ExpectedStatus = 200,
        ExpectedContentContains = "token",
        Points = 4   // ~0.4 pts
    },
    new()
    {
        Name = "Login success → response includes role",
        Method = HttpMethod.Post,
        Path = "/api/auth",
        JsonBody = """{"email":"administrator@leopard.com","password":"@1"}""",
        ExpectedStatus = 200,
        ExpectedContentContains = "role",
        Points = 4   // ~0.4 pts
    },
    new()
    {
        Name = "Login wrong password → 401 or 404",
        Method = HttpMethod.Post,
        Path = "/api/auth",
        JsonBody = """{"email":"administrator@leopard.com","password":"wrongpass"}""",
        ExpectedStatus = 401,
        AlternateStatus = 404,
        Points = 3   // ~0.3 pts
    },
    new()
    {
        Name = "Login non-existent email → 401 or 404",
        Method = HttpMethod.Post,
        Path = "/api/auth",
        JsonBody = """{"email":"notexist@no.com","password":"@1"}""",
        ExpectedStatus = 401,
        AlternateStatus = 404,
        Points = 3   // ~0.3 pts
    },

    // ── 2. LeopardProfile API Endpoints  (~6.92 pts total) ────────────

    // GET list
    new()
    {
        Name = "GET /api/LeopardProfile – no token → 401",
        Method = HttpMethod.Get,
        Path = "/api/LeopardProfile",
        ExpectedStatus = 401,
        Points = 5   // ~0.5 pts
    },
    new()
    {
        Name = "GET /api/LeopardProfile – administrator → 200 with data",
        Method = HttpMethod.Get,
        Path = "/api/LeopardProfile",
        BearerTokenEmail = "administrator@leopard.com",
        BearerTokenPassword = "@1",
        ExpectedStatus = 200,
        ExpectedContentContains = "LeopardName",
        Points = 5   // ~0.5 pts
    },
    new()
    {
        Name = "GET /api/LeopardProfile – moderator → 200",
        Method = HttpMethod.Get,
        Path = "/api/LeopardProfile",
        BearerTokenEmail = "moderator@leopard.com",
        BearerTokenPassword = "@1",
        ExpectedStatus = 200,
        Points = 3
    },
    new()
    {
        Name = "GET /api/LeopardProfile – developer → 200",
        Method = HttpMethod.Get,
        Path = "/api/LeopardProfile",
        BearerTokenEmail = "developer@leopard.com",
        BearerTokenPassword = "@1",
        ExpectedStatus = 200,
        Points = 3
    },
    new()
    {
        Name = "GET /api/LeopardProfile – member → 200",
        Method = HttpMethod.Get,
        Path = "/api/LeopardProfile",
        BearerTokenEmail = "member@leopard.com",
        BearerTokenPassword = "@1",
        ExpectedStatus = 200,
        Points = 3
    },

    // GET by ID
    new()
    {
        Name = "GET /api/LeopardProfile/1 – administrator → 200",
        Method = HttpMethod.Get,
        Path = "/api/LeopardProfile/1",
        BearerTokenEmail = "administrator@leopard.com",
        BearerTokenPassword = "@1",
        ExpectedStatus = 200,
        Points = 3
    },
    new()
    {
        Name = "GET /api/LeopardProfile/999999 – not found → 404",
        Method = HttpMethod.Get,
        Path = "/api/LeopardProfile/999999",
        BearerTokenEmail = "administrator@leopard.com",
        BearerTokenPassword = "@1",
        ExpectedStatus = 404,
        Points = 3
    },

    // POST create
    new()
    {
        Name = "POST /api/LeopardProfile – administrator → 200/201",
        Method = HttpMethod.Post,
        Path = "/api/LeopardProfile",
        BearerTokenEmail = "administrator@leopard.com",
        BearerTokenPassword = "@1",
        JsonBody = """{"LeopardName":"GraderLeoAdmin","LeopardTypeId":1,"Weight":50,"Characteristics":"Test cat","CareNeeds":"Monitored","ModifiedDate":"2025-06-20T00:00:00"}""",
        ExpectedStatus = 201,
        AlternateStatus = 200,
        Points = 5
    },
    new()
    {
        Name = "POST /api/LeopardProfile – moderator → 200/201",
        Method = HttpMethod.Post,
        Path = "/api/LeopardProfile",
        BearerTokenEmail = "moderator@leopard.com",
        BearerTokenPassword = "@1",
        JsonBody = """{"LeopardName":"GraderLeoMod","LeopardTypeId":1,"Weight":60,"Characteristics":"Test","CareNeeds":"Protected","ModifiedDate":"2025-06-20T00:00:00"}""",
        ExpectedStatus = 201,
        AlternateStatus = 200,
        Points = 5
    },
    new()
    {
        Name = "POST /api/LeopardProfile – member → 403",
        Method = HttpMethod.Post,
        Path = "/api/LeopardProfile",
        BearerTokenEmail = "member@leopard.com",
        BearerTokenPassword = "@1",
        JsonBody = """{"LeopardName":"Denied","LeopardTypeId":1,"Weight":50,"Characteristics":"x","CareNeeds":"x","ModifiedDate":"2025-06-20T00:00:00"}""",
        ExpectedStatus = 403,
        Points = 5
    },
    new()
    {
        Name = "POST /api/LeopardProfile – developer → 403",
        Method = HttpMethod.Post,
        Path = "/api/LeopardProfile",
        BearerTokenEmail = "developer@leopard.com",
        BearerTokenPassword = "@1",
        JsonBody = """{"LeopardName":"DevDenied","LeopardTypeId":1,"Weight":50,"Characteristics":"x","CareNeeds":"x","ModifiedDate":"2025-06-20T00:00:00"}""",
        ExpectedStatus = 403,
        Points = 4
    },

    // PUT update
    new()
    {
        Name = "PUT /api/LeopardProfile/2 – administrator → 200/201/204",
        Method = HttpMethod.Put,
        Path = "/api/LeopardProfile/2",
        BearerTokenEmail = "administrator@leopard.com",
        BearerTokenPassword = "@1",
        JsonBody = """{"LeopardName":"UpdatedLeo","LeopardTypeId":1,"Weight":55,"Characteristics":"Updated","CareNeeds":"Updated care","ModifiedDate":"2025-06-20T00:00:00"}""",
        ExpectedStatus = 200,
        AlternateStatus = 201,
        Points = 5
    },
    new()
    {
        Name = "PUT /api/LeopardProfile/2 – administrator → 204 (alt)",
        Method = HttpMethod.Put,
        Path = "/api/LeopardProfile/2",
        BearerTokenEmail = "administrator@leopard.com",
        BearerTokenPassword = "@1",
        JsonBody = """{"LeopardName":"UpdatedLeo","LeopardTypeId":1,"Weight":55,"Characteristics":"Updated","CareNeeds":"Updated care","ModifiedDate":"2025-06-20T00:00:00"}""",
        ExpectedStatus = 204,
        Points = 0   // 0 pts – prevents double-scoring; 204 is an accepted alternate only
    },
    new()
    {
        Name = "PUT /api/LeopardProfile/3 – moderator → 200/201/204",
        Method = HttpMethod.Put,
        Path = "/api/LeopardProfile/3",
        BearerTokenEmail = "moderator@leopard.com",
        BearerTokenPassword = "@1",
        JsonBody = """{"LeopardName":"ModUpdated","LeopardTypeId":1,"Weight":45,"Characteristics":"Mod updated","CareNeeds":"Care","ModifiedDate":"2025-06-20T00:00:00"}""",
        ExpectedStatus = 200,
        AlternateStatus = 201,
        Points = 4
    },

    // DELETE
    new()
    {
        Name = "DELETE /api/LeopardProfile/4 – administrator → 200/204",
        Method = HttpMethod.Delete,
        Path = "/api/LeopardProfile/4",
        BearerTokenEmail = "administrator@leopard.com",
        BearerTokenPassword = "@1",
        ExpectedStatus = 200,
        AlternateStatus = 204,
        PathHint = "delete_4",
        Points = 4
    },
    new()
    {
        Name = "DELETE /api/LeopardProfile/5 – developer → 403",
        Method = HttpMethod.Delete,
        Path = "/api/LeopardProfile/5",
        BearerTokenEmail = "developer@leopard.com",
        BearerTokenPassword = "@1",
        ExpectedStatus = 403,
        PathHint = "delete_5",
        Points = 5
    },

    // ── 3. Error Code Format HB400001  (~1.54 pts total) ──────────────
    new()
    {
        Name = "POST weight ≤ 15 → 400 + error code HB400001",
        Method = HttpMethod.Post,
        Path = "/api/LeopardProfile",
        BearerTokenEmail = "administrator@leopard.com",
        BearerTokenPassword = "@1",
        JsonBody = """{"LeopardName":"TinyLeo","LeopardTypeId":1,"Weight":10,"Characteristics":"x","CareNeeds":"x"}""",
        ExpectedStatus = 400,
        ExpectedContentContains = "HB400001",
        Points = 15  // full 1.5 pts allocated here as sole test for this section
    },
};

    //private static
    public int GetFreePort(int start = 5100, int end = 5200)
    {
        for (int port = start; port <= end; port++)
        {
            try
            {
                using var listener = new TcpListener(System.Net.IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
                return port;
            }
            catch { }
        }
        throw new Exception($"No free port found in range {start}-{end}");
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