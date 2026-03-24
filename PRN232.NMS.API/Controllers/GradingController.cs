using Grader.Services;
using Microsoft.AspNetCore.Mvc;
using PRN232.NMS.API.Models.ResponseModels;
using PRN232.NMS.Repo.Entities;
using PRN232.NMS.Services;
using PRN232.NMS.Services.Helpers.HelperEntities;
using PRN232.NMS.Services.Models.RequestModels;
using PRN232.NMS.Services.Models.ResponseModels;

namespace PRN232.NMS.API.Controllers
{
    [ApiController]
    [Route("api/students")]
    public class GradingController : ControllerBase
    {
        private readonly GradingService _grader;

        public GradingController(GradingService grader) => _grader = grader;

        [HttpPost("single-directory")]
        public async Task<IActionResult> Grade([FromBody] GradingRequest req)
        {
            var result = await _grader.GradeAsync(req);

            var response = new ResponseDTO<GradingResultSingleResponse>(message: "Graded successfully", isSuccess: true, data: result, errors: null);

            return Ok(response);
        }

        [HttpPost("all-directory")]
        public async Task<IActionResult> GradeAllAsync()
        {
            var result = await _grader.GradeAllAsync();

            var response = new ResponseDTO<GradingResultAllResponse>(message: "Graded successfully", isSuccess: true, data: result, errors: null);

            return Ok(response);
        }
    }
}
