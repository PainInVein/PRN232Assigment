using Azure;
using Microsoft.AspNetCore.Mvc;
using PRN232.NMS.API.Models.ResponseModels;
using PRN232.NMS.Services.Models.RequestModels;
using PRN232.NMS.Services.Models.ResponseModels;
using PRN232.NMS.Services.Services;
using System.ComponentModel.DataAnnotations;

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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute][Range(1, int.MaxValue, ErrorMessage = "StudentId must be greater than 0")] int id)
        {
            var result = await _folderService.GetByIdAsync(id);
            if (result == null)
            {
                return NotFound(new ResponseDTO<object>(message: "Submission not found", isSuccess: false, data: null, errors: $"Resource with id: {id} not found"));
            }

            return Ok(new ResponseDTO<object>(message: "Submission found", isSuccess: true, data: result, errors: null));
        }

        [HttpPut("{id}/path")]
        public async Task<IActionResult> UpdateFolderPath([FromRoute][Range(1, int.MaxValue, ErrorMessage = "StudentId must be greater than 0")] int id,
            [FromBody] SubmissionPathUpdateRequest updateSubmissionPathRequest)
        {
            var result = await _folderService.UpdateFolderPathAsync(id, updateSubmissionPathRequest.ProjectFolder);
            if (!string.IsNullOrEmpty(result))
            {
                return NotFound(new ResponseDTO<object>(message: result, isSuccess: false, data: null, errors: null));
            }
            return Ok(new ResponseDTO<object>(message: "Folder path updated successfully", isSuccess: true, data: null, errors: null));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubmission([FromRoute][Range(1, int.MaxValue, ErrorMessage = "Submission Id must be greater than 0")] int id)
        {
            var result = await _folderService.HardDeleteSubmissionAsync(id);
            if (!string.IsNullOrEmpty(result))
            {
                return NotFound(new ResponseDTO<object>(message: result, isSuccess: false, data: null, errors: null));
            }
            return Ok(new ResponseDTO<object>(message: "Submission deleted successfully", isSuccess: true, data: null, errors: null));
        }
    }
}