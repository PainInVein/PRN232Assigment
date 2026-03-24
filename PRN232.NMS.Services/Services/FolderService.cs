using AutoMapper;
using Microsoft.Extensions.Options;
using PRN232.NMS.Repo.Entities;
using PRN232.NMS.Services.BusinessModel;
using PRN232.NMS.Services.Models.ResponseModels;
using Repositories;

namespace PRN232.NMS.Services.Services
{
    public class FolderService
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly DatabaseSettings _dbSettings;
        private readonly IMapper _mapper;

        public FolderService(IUnitOfWork unitOfWork, IOptions<DatabaseSettings> dbSettings, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _dbSettings = dbSettings.Value;
            _mapper = mapper;
        }

        private string ResolvePathToDockerPath(string path)
        {
            string windowsPrefix = _dbSettings.WindowsPrefixPath;
            string dockerPrefix = _dbSettings.StudentBasePath;
            if (path.StartsWith(windowsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return path.Replace(windowsPrefix, dockerPrefix)
                           .Replace("\\", "/");
            }

            return path;
        }

        private string ResolvePathToWindowPath(string path)
        {
            string dockerPrefix = _dbSettings.StudentBasePath;
            string windowsPrefix = _dbSettings.WindowsPrefixPath;

            if (path.StartsWith(dockerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return path.Replace(dockerPrefix, windowsPrefix)
                           .Replace("/", "\\");
            }

            return path;
        }

        public async Task<List<SubfolderInfo>> GetSubfolders(string inputFolderPath)
        {

            var result = new List<SubfolderInfo>();
            var submissionData = new List<GradingResult>();

            string projectFolder = ResolvePathToDockerPath(inputFolderPath);

            try
            {
                if (string.IsNullOrWhiteSpace(projectFolder))
                {
                    throw new ArgumentException("Project folder path cannot be empty or whitespace.");
                }

                projectFolder = projectFolder.Trim();
                projectFolder = Path.GetFullPath(projectFolder);

                var invalidChars = Path.GetInvalidPathChars();
                if (projectFolder.Any(c => invalidChars.Contains(c)))
                {
                    throw new ArgumentException($"Invalid characters in folder path: '{projectFolder}'.");
                }

                bool folderExists = Directory.Exists(projectFolder);

                if (!folderExists)
                {
                    throw new DirectoryNotFoundException($"The folder '{projectFolder}' does not exist.");
                }

                string[] subdirectoryPaths = Directory.GetDirectories(projectFolder);

                foreach (var subdirPath in subdirectoryPaths)
                {
                    var dirInfo = new DirectoryInfo(subdirPath);
                    result.Add(new SubfolderInfo
                    {
                        FolderName = dirInfo.Name,
                        Path = ResolvePathToWindowPath(dirInfo.FullName)
                    });
                    submissionData.Add(new GradingResult
                    {
                        StudentName = dirInfo.Name,
                        ProjectFolder = ResolvePathToWindowPath(dirInfo.FullName),
                        Score = 0,
                        Logs = null,
                        Points = 0,
                        Status = "Pending"
                    });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedAccessException($"Access denied to folder '{projectFolder}': {ex.Message}", ex);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (DirectoryNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"An error occurred while reading subfolders from '{projectFolder}': {ex.Message}", ex);
            }

            await _unitOfWork.GradingResultRepository.CreateRangeAsync(submissionData);

            return result;
        }

        public async Task<(List<SubmissionsGetAllResponse> Items, int TotalItems)> GetSubmissionInfoPagedAsync(int page, int pageSize, string? searchTerm, string? sortOption, List<string>? status)
        {
            try
            {
                var items = await _unitOfWork.GradingResultRepository
                    .GetAllSubmissionsAsync((page - 1) * pageSize, pageSize, searchTerm, sortOption, status);

                var returnItem = _mapper.Map<List<SubmissionsGetAllResponse>>(items.Items);

                return (returnItem, items.TotalItems);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<GetSubmissionByIdResponse?> GetByIdAsync(int id)
        {
            var result = await _unitOfWork.GradingResultRepository.GetByIdDetailedAsync(id);

            if (result == null)
                return null;

            return _mapper.Map<GetSubmissionByIdResponse>(result);
        }

        public async Task<string> UpdateFolderPathAsync(int id, string folderPath)
        {
            var existingPath = await _unitOfWork.GradingResultRepository.GetByIdAsync(id);
            if (existingPath == null) return $"Submission with {id} not found";

            string projectPath = ResolvePathToDockerPath(folderPath);
            projectPath = projectPath.Trim();
            projectPath = Path.GetFullPath(projectPath);

            bool folderExists = Directory.Exists(projectPath);

            if (!folderExists) return $"The folder '{projectPath}' does not exist.";

            var dirInfo = new DirectoryInfo(projectPath);

            var updatedPath = ResolvePathToWindowPath(dirInfo.FullName);

            existingPath.ProjectFolder = updatedPath;
            _unitOfWork.GradingResultRepository.Update(existingPath);
            return string.Empty;
        }

        public async Task<string> HardDeleteSubmissionAsync(int id)
        {
            var existingPath = await _unitOfWork.GradingResultRepository.GetByIdAsync(id);
            if (existingPath == null) return $"Submission with {id} not found";

            await _unitOfWork.GradingResultRepository.RemoveAsync(existingPath);

            return string.Empty;
        }
    }
}
