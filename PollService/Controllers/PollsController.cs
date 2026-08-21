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
        private readonly IConfiguration _config;

        public PollsController(IPollService polls, IConfiguration config)
        {
            _polls = polls;
            _config = config;
        }

        [HttpPost]
        public async Task<ActionResult<PollDto>> Create([FromBody] CreatePollRequest request)
        {
            try
            {
                var poll = await _polls.CreatePollAsync(request.Question, request.Options);

                // Đồng bộ poll mới sang VoteService (không làm fail API nếu VoteService lỗi)
                try
                {
                    var voteServiceUrl = _config["VoteServiceUrl"] ?? "https://pollbuilder-voteservice-nbjl.onrender.com";
                    using var http = new HttpClient();
                    await http.PostAsJsonAsync($"{voteServiceUrl}/api/internal/polls", new
                    {
                        Code = poll.Code,
                        Question = poll.Question,
                        Options = request.Options
                    });
                }
                catch
                {
                    // Bỏ qua nếu VoteService tạm thời không phản hồi, không làm fail API tạo poll
                }

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
            try
            {
                var realtimeUrl = _config["RealtimeServiceUrl"] ?? "https://pollbuilder-realtimeservice.onrender.com";
                using var http = new HttpClient();
                await http.PostAsJsonAsync($"{realtimeUrl}/api/notify/close", new { Code = code });
            }
            catch
            {
            }
            return Ok(poll);
        }
    }
}
