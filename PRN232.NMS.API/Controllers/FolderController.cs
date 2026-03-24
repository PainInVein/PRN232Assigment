using Microsoft.AspNetCore.Mvc;
using PRN232.NMS.API.Models.ResponseModels;
using PRN232.NMS.Services.Models.RequestModels;
using PRN232.NMS.Services.Models.ResponseModels;
using PRN232.NMS.Services.Services;

namespace PRN232.NMS.API.Controllers
{
    [ApiController]
    [Route("api/folders")]
    public class FolderController : ControllerBase
    {
        private readonly FolderService _folderService;

        public FolderController(FolderService folderService)
        {
            _folderService = folderService;
        }

        [HttpPost]
        public async Task<IActionResult> GetFolders([FromBody] GetSubfoldersRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.ProjectFolder))
                {
                    return BadRequest(new { error = "ProjectFolder is required and cannot be empty." });
                }

                var subfolders = await _folderService.GetSubfolders(request.ProjectFolder);

                return Ok(subfolders);
            }
            catch (DirectoryNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSubmissions([FromQuery] SubmissionFilterRequest submissionFilterRequest)
        {

            var pagedSubmissions = await _folderService.GetSubmissionInfoPagedAsync(submissionFilterRequest.Page, submissionFilterRequest.PageSize, submissionFilterRequest.SearchName, submissionFilterRequest.SortOption, submissionFilterRequest.StatusList);

            var pagedResponse = new PagedResult<SubmissionsGetAllResponse>
            {
                Items = pagedSubmissions.Items,
                Page = submissionFilterRequest.Page,
                PageSize = submissionFilterRequest.PageSize,
                TotalItems = pagedSubmissions.TotalItems,
                TotalPages = (int)Math.Ceiling(pagedSubmissions.TotalItems / (double)submissionFilterRequest.PageSize)
            };

            var response = new ResponseDTO<PagedResult<SubmissionsGetAllResponse>>(message: "Submissions retrieved successfully", isSuccess: true, data: pagedResponse, errors: null);

            return Ok(response);
        }
    }
}