using AutoMapper;
using Grader.Services;
using Microsoft.AspNetCore.Mvc;
using PRN232.NMS.API.Models.ResponseModels;
using PRN232.NMS.Services;

namespace PRN232.NMS.API.Controllers
{
    [ApiController]
    [Route("api/grade")]
    public class GradingController : ControllerBase
    {
        private readonly GradingService _grader;
        private readonly IMapper _mapper;

        public GradingController(GradingService grader, IMapper mapper)
        {
            _grader = grader;
            _mapper = mapper;
        }

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

        [HttpGet("All-Score")]
        public async Task<IActionResult> GetAllScoreAsync()
        {
            var result = await _grader.GetAllGradingResultsAsync();
            return Ok(_mapper.Map<IEnumerable<GradingResultDTO>>(result));
        }
    }
}
