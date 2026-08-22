using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Security.Claims;
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

        // Hàm phụ lấy UserId an toàn (hỗ trợ cả nameid, sub, và ClaimTypes)
        private bool TryGetUserId(out Guid userId)
        {
            userId = Guid.Empty;
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? User.FindFirst("nameid")?.Value 
                     ?? User.FindFirst("sub")?.Value;

            return !string.IsNullOrEmpty(claim) && Guid.TryParse(claim, out userId);
        }

        // TẠO POLL
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<PollDto>> Create([FromBody] CreatePollRequest request)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { error = "Không xác định được người dùng." });
            }

            try
            {
                var poll = await _polls.CreatePollAsync(request.Question, request.Options, userId);

                // Đồng bộ poll mới sang VoteService
                try
                {
                    var baseUrl = (_config["VoteServiceUrl"] ?? "https://pollbuilder-voteservice.onrender.com").TrimEnd('/');
                    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

                    var response = await http.PostAsJsonAsync($"{baseUrl}/api/internal/polls", new
                    {
                        Code = poll.Code,
                        Question = poll.Question,
                        Options = request.Options
                    });

                    if (!response.IsSuccessStatusCode)
                    {
                        var errBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[SYNC ERROR] VoteService returned {response.StatusCode}: {errBody}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SYNC EXCEPTION] Could not connect to VoteService: {ex.Message}");
                }

                return CreatedAtAction(nameof(Get), new { code = poll.Code }, poll);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // LỊCH SỬ POLL CỦA TÔI
        [Authorize]
        [HttpGet("my-polls")]
        public async Task<ActionResult<IEnumerable<PollDto>>> GetMyPolls()
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { error = "Không xác định được người dùng." });
            }

            var myPolls = await _polls.GetPollsByUserAsync(userId);
            return Ok(myPolls);
        }

        // LẤY 1 POLL THEO MÃ (chỉ mã 6 ký tự A-Z0-9)
        [HttpGet("{code:regex(^[[A-Z0-9]]{{6}}$)}")]
        public async Task<ActionResult<PollDto>> Get(string code)
        {
            var poll = await _polls.GetPollAsync(code);
            return poll is null
                ? NotFound(new { error = "Poll not found." })
                : Ok(poll);
        }

        [HttpGet("{code:regex(^[[A-Z0-9]]{{6}}$)}/results")]
        public async Task<ActionResult<PollResultsDto>> Results(string code)
        {
            var results = await _polls.GetResultsAsync(code);
            return results is null
                ? NotFound(new { error = "Poll not found." })
                : Ok(results);
        }

        // ĐÓNG POLL
        [Authorize]
        [HttpPatch("{code:regex(^[[A-Z0-9]]{{6}}$)}/close")]
        public async Task<ActionResult<PollDto>> Close(string code)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { error = "Không xác định được người dùng." });
            }

            var pollCheck = await _polls.GetPollAsync(code);
            if (pollCheck == null) return NotFound(new { error = "Poll not found." });

            if (pollCheck.CreatorId != userId)
            {
                return StatusCode(403, new { error = "Bạn không có quyền đóng Poll của người khác!" });
            }

            var poll = await _polls.ClosePollAsync(code);

            try
            {
                var realtimeUrl = (_config["RealtimeServiceUrl"] ?? "https://pollbuilder-realtimeservice.onrender.com").TrimEnd('/');
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                await http.PostAsJsonAsync($"{realtimeUrl}/api/notify/close", new { Code = code });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REALTIME ERROR] Failed to notify close: {ex.Message}");
            }

            return Ok(poll);
        }
    }
}
