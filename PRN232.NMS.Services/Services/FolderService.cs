using PRN232.NMS.Repo.Entities;
using Repositories;
using System.Threading.Tasks;

namespace PRN232.NMS.Services.Services
{
    public class FolderService
    {

        private readonly IUnitOfWork _unitOfWork;

        public FolderService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private string ResolvePathToDockerPath(string path)
        {
            const string windowsPrefix = @"C:\Users\Admin\Desktop\submissions";
            if (path.StartsWith(windowsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return path.Replace(windowsPrefix, "/students")
                           .Replace("\\", "/");
            }

            return path;
        }

        private string ResolvePathToWindowPath(string path)
        {
            const string dockerPrefix = "/students";
            const string windowsPrefix = @"C:\Users\Admin\Desktop\submissions";

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
    }
}
