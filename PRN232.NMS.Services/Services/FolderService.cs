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

        public async Task<List<SubfolderInfo>> GetSubfolders(string projectFolder)
        {

            var result = new List<SubfolderInfo>();
            var submissionData = new List<GradingResult>();

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
                        Path = dirInfo.FullName
                    });
                    submissionData.Add(new GradingResult
                    {
                        StudentName = dirInfo.Name,
                        ProjectFolder = dirInfo.FullName,
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
