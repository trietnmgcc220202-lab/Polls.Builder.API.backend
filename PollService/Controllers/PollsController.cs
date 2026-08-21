using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using PollService.Contracts;
using PollService.Services;

namespace PollService.Controllers
{
    [ApiController]
    [Route("api/polls")]
    public class PollsController : ControllerBase
    {
        private readonly IPollService _polls;

        public PollsController(IPollService polls)
        {
            _polls = polls;
        }

        [HttpPost]
        public async Task<ActionResult<PollDto>> Create([FromBody] CreatePollRequest request)
        {
            try
            {
                var poll = await _polls.CreatePollAsync(request.Question, request.Options);
                return CreatedAtAction(nameof(Get), new { code = poll.Code }, poll);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{code}")]
        public async Task<ActionResult<PollDto>> Get(string code)
        {
            var poll = await _polls.GetPollAsync(code);
            return poll is null
                ? NotFound(new { error = "Poll not found." })
                : Ok(poll);
        }

        [HttpGet("{code}/results")]
        public async Task<ActionResult<PollResultsDto>> Results(string code)
        {
            var results = await _polls.GetResultsAsync(code);
            return results is null
                ? NotFound(new { error = "Poll not found." })
                : Ok(results);
        }

        [HttpPatch("{code}/close")]
        public async Task<ActionResult<PollDto>> Close(string code)
        {
            var poll = await _polls.ClosePollAsync(code);
            if (poll is null)
            {
                return NotFound(new { error = "Poll not found." });
            }

            // Gửi thông báo đóng poll sang RealtimeService
            try
            {
                using var http = new HttpClient();
                await http.PostAsJsonAsync("http://localhost:5003/api/notify/close", new { Code = code });
            }
            catch
            {
                // Bỏ qua nếu RealtimeService chưa chạy, không làm fail API close
            }

            return Ok(poll);
        }
    }
}