using Grader.Services;
using Microsoft.AspNetCore.Mvc;
using PRN232.NMS.Services;

namespace PRN232.NMS.API.Controllers
{
    [ApiController]
    [Route("api/grade")]
    public class GradingController : ControllerBase
    {
        private readonly GradingService _grader;

        public GradingController(GradingService grader) => _grader = grader;

        [HttpPost("single-directory")]
        public async Task<IActionResult> Grade([FromBody] GradingRequest req)
        {
            var result = await _grader.GradeAsync(req);
            return Ok(result);
        }

        [HttpPost("all-directory")]
        public async Task<IActionResult> GradeAllAsync()
        {
            var result = await _grader.GradeAllAsync();
            return Ok(result);
        }
    }
}
