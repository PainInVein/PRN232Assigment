using Grader.Services;
using Microsoft.AspNetCore.Mvc;
using PRN232.NMS.API.Models.ResponseModels;
using PRN232.NMS.Repo.Entities;
using PRN232.NMS.Services;
using PRN232.NMS.Services.Helpers.HelperEntities;
using PRN232.NMS.Services.Models.RequestModels;
using PRN232.NMS.Services.Models.ResponseModels;
using System.ComponentModel.DataAnnotations;

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

        [HttpPost("single-directory/{id}")]
        public async Task<IActionResult> RegradeAsync([FromRoute][Range(1, int.MaxValue, ErrorMessage = "StudentId must be greater than 0")] int id)
        {
            var result = await _grader.GradeByIdAsync(id);
            var response = new ResponseDTO<GradingResultSingleResponse>(message: "Regraded successfully", isSuccess: true, data: result, errors: null);
            return Ok(response);
        }
    }
}
