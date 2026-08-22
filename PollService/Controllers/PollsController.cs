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

        // 1. TẠO POLL (Bạn làm chuẩn rồi, giữ nguyên)
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<PollDto>> Create([FromBody] CreatePollRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { error = "Không xác định được người dùng." });
            }

            try
            {
                var poll = await _polls.CreatePollAsync(request.Question, request.Options, userId);

                // Đồng bộ poll mới sang VoteService
                try
                {
                    var baseUrl = (_config["VoteServiceUrl"] ?? "https://pollbuilder-voteservice-nbjl.onrender.com").TrimEnd('/');
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

        // --------------------------------------------------------
        // BỔ SUNG MỚI: API XEM LỊCH SỬ POLL CỦA TÔI
        // --------------------------------------------------------
        [Authorize]
        [HttpGet("my-polls")]
        public async Task<ActionResult<IEnumerable<PollDto>>> GetMyPolls()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { error = "Không xác định được người dùng." });
            }

            // Gọi service lấy danh sách poll theo ID người dùng
            var myPolls = await _polls.GetPollsByUserAsync(userId);
            return Ok(myPolls);
        }
        // --------------------------------------------------------


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

        // --------------------------------------------------------
        // CẬP NHẬT LẠI: API ĐÓNG POLL (Chặn người lạ đóng)
        // --------------------------------------------------------
        [Authorize]
        [HttpPatch("{code}/close")]
        public async Task<ActionResult<PollDto>> Close(string code)
        {
            // Lấy ID người đang thực hiện request
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { error = "Không xác định được người dùng." });
            }

            // Lấy thông tin Poll ra trước để kiểm tra quyền
            var pollCheck = await _polls.GetPollAsync(code);
            if (pollCheck == null) return NotFound(new { error = "Poll not found." });

            // Kiểm tra chủ sở hữu
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
