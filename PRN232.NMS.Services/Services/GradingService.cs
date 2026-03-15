using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PRN232.NMS.Repo.DBContext;
using PRN232.NMS.Repo.Entities;
using PRN232.NMS.Services;
using PRN232.NMS.Services.Helpers.HelperClasses;
using PRN232.NMS.Services.Helpers.HelperEntities;
using PRN232.NMS.Services.Services;
using Repositories;
using System.Diagnostics;
using System.Runtime;

namespace Grader.Services;

public class GradingService
{
    private readonly ILogger<GradingService> _logger;
    private readonly IClassHelperFacade _helperFacade;
    private readonly ExecuteTestService _executeTestService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DatabaseSettings _dbSettings;


    public GradingService(
        ILogger<GradingService> logger,
        IConfiguration config,
        IClassHelperFacade helperFacade,
        ExecuteTestService executeTestService,
        IUnitOfWork unitOfWork,
        IOptions<DatabaseSettings> dbSettings
        )
    {
        _logger = logger;
        _helperFacade = helperFacade;
        _executeTestService = executeTestService;
        _unitOfWork = unitOfWork;
        _dbSettings = dbSettings.Value;
    }

    // Service cham diem chinh, tra ve chi tiet ket qua de hien thi tren UI, va luu vao database
    public async Task<GradingResultWithListLogs> GradeAsync(GradingRequest req, CancellationToken ct = default)
    {
        var result = new GradingResultWithListLogs
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
            var baseTempFolder = _dbSettings.BaseTempFolder;
            tempDir = Path.Combine(baseTempFolder, $"grade-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            _helperFacade.CopyDirectory(req.ProjectFolder, tempDir);
            result.Logs.Add("Project copied to temporary folder");

            // Chỗ này tạo database tạm thời cho sinh viên
            var guidPart = Guid.NewGuid().ToString("N")[..8];
            dbName = $"Grade_{DateTime.UtcNow:yyyyMMddHHmmss}_{guidPart}";
            await _helperFacade.CreateDatabaseAsync(dbName, _dbSettings.MasterConnectionString);
            result.Logs.Add($"Database {dbName} created");

            var studentConnStr = _helperFacade.BuildStudentConnectionString(dbName, _dbSettings.UserId, _dbSettings.Password, _dbSettings.ServerName);
            await _helperFacade.ApplySchemaAsync(dbName, _dbSettings.SchemaPath, _dbSettings.UserId, _dbSettings.Password, _dbSettings.ServerName);
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
            var outcomes = await _executeTestService.ExecuteApiTestsAsync(baseUrl, result.Logs, routeMap);
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

            if (dbName != null) await _helperFacade.TryDropDatabaseAsync(dbName, _dbSettings.MasterConnectionString);
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
            await _unitOfWork.GradingResultRepository.CreateAsync(mappedResult);
        }

        return result;
    }



     //Service chấm điểm tất cả submission
    public async Task<GradingAllResult> GradeAllAsync(CancellationToken ct = default)
    {
        var overallResult = new GradingAllResult
        {
            ProjectFolder = "ALL_SUBMISSIONS",
            Logs = new List<string> { $"Started grading ALL submissions at {DateTimeOffset.Now:HH:mm:ss}" }
        };

        List<GradingResult> allSubmissions = await _unitOfWork.GradingResultRepository.GetAllAsync();

        foreach (var submission in allSubmissions)
        {
            string? tempDir = null;
            string? dbName = null;
            Process? apiProcess = null;

            var logs = new List<string> { $"Started grading {submission.ProjectFolder} at {DateTimeOffset.Now:HH:mm:ss}" };
            var perResult = new GradingResultWithListLogs
            {
                ProjectFolder = submission.ProjectFolder,
                Logs = logs
            };

            try
            {
                var baseTempFolder = _dbSettings.BaseTempFolder;
                tempDir = Path.Combine(baseTempFolder, $"grade-{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);

                _helperFacade.CopyDirectory(submission.ProjectFolder, tempDir);
                logs.Add("Project copied to temporary folder");

                var guidPart = Guid.NewGuid().ToString("N")[..8];
                dbName = $"Grade_{DateTime.UtcNow:yyyyMMddHHmmss}_{guidPart}";
                await _helperFacade.CreateDatabaseAsync(dbName, _dbSettings.MasterConnectionString);
                logs.Add($"Database {dbName} created");

                var studentConnStr = _helperFacade.BuildStudentConnectionString(dbName, _dbSettings.UserId, _dbSettings.Password, _dbSettings.ServerName);
                await _helperFacade.ApplySchemaAsync(dbName, _dbSettings.SchemaPath, _dbSettings.UserId, _dbSettings.Password, _dbSettings.ServerName);
                logs.Add("Schema applied");

                await _helperFacade.PatchConnectionStringAsync(tempDir, studentConnStr);
                logs.Add("Connection string updated");

                bool buildOk = await _helperFacade.BuildAsync(tempDir, logs);
                if (!buildOk)
                {
                    perResult.Status = "BuildFailed";
                    perResult.Score = 0;
                    logs.Add("Build failed");
                }
                else
                {
                    int port = _helperFacade.GetFreePort(5100, 5200);
                    string baseUrl = $"http://localhost:{port}";
                    apiProcess = _helperFacade.StartApi(tempDir, port, logs);

                    bool started = await _helperFacade.WaitForApiReadyAsync(baseUrl, ct);
                    if (!started)
                    {
                        perResult.Status = "StartupTimeout";
                        perResult.Score = 0;
                        logs.Add("Startup timeout - API did not respond");
                    }
                    else
                    {
                        logs.Add($"API responding on {baseUrl}");

                        var routeMap = await _helperFacade.DiscoverRoutesAsync(baseUrl, logs);
                        var outcomes = await _executeTestService.ExecuteApiTestsAsync(baseUrl, logs, routeMap);

                        perResult.Score = outcomes.Where(o => o.Passed).Sum(o => o.Points);
                        logs.AddRange(outcomes.Select(o => $"{o.Name,-32} {(o.Passed ? "PASS" : "FAIL"),-6} {o.Points,3} pts {o.Message ?? ""}"));
                        perResult.Status = perResult.Score >= 5 ? "Passed" : "Failed";
                    }
                }
            }
            catch (Exception ex)
            {
                perResult.Status = "Exception";
                perResult.Score = 0;
                logs.Add($"CRITICAL: {ex.GetType().Name}: {ex.Message}");
                _logger.LogError(ex, $"Grading crashed for submission {submission.ProjectFolder}");
            }
            finally
            {
                // Cleanup
                if (apiProcess is { HasExited: false })
                {
                    try { apiProcess.Kill(true); } catch { }
                    logs.Add("API process terminated");
                }
                if (dbName != null)
                    await _helperFacade.TryDropDatabaseAsync(dbName, _dbSettings.MasterConnectionString);

                if (tempDir != null && Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); }
                    catch { logs.Add("Warning: could not delete temp folder"); }
                }

                perResult.FinishedAt = DateTime.UtcNow;

                // Update entity
                submission.Status = perResult.Status ?? "Unknown";
                submission.Score = perResult.Score;
                submission.Logs = string.Join("\n", logs);


                await _unitOfWork.GradingResultRepository.UpdateAsync(submission);

                _helperFacade.ClearTokenCache();
            }
        }

        overallResult.Logs.Add($"Finished grading {allSubmissions.Count} submissions at {DateTimeOffset.Now:HH:mm:ss}");
        overallResult.Status = "AllProcessed";
        return overallResult;
    }
}