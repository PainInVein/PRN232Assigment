using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PRN232.NMS.Repo.DBContext;
using PRN232.NMS.Repo.Entities;
using PRN232.NMS.Services;
using PRN232.NMS.Services.Helpers.HelperClasses;
using PRN232.NMS.Services.Helpers.HelperEntities;
using PRN232.NMS.Services.Services;
using Repositories;
using System.Diagnostics;

namespace Grader.Services;

public class GradingService
{
    private readonly ILogger<GradingService> _logger;
    private readonly IClassHelperFacade _helperFacade;
    private readonly ExecuteTestService _executeTestService;
    private readonly IUnitOfWork _unitOfWork;

    public GradingService(
        ILogger<GradingService> logger,
        IConfiguration config,
        Prn232lab3Context graderDb,
        IClassHelperFacade helperFacade,
        ExecuteTestService executeTestService,
        IUnitOfWork unitOfWork
        )
    {
        _logger = logger;
        _helperFacade = helperFacade;
        _executeTestService = executeTestService;
        _unitOfWork = unitOfWork;
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
            await _unitOfWork.GradingResultRepository.CreateAsync(mappedResult);
        }

        return result;
    }
}