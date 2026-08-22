using Microsoft.AspNetCore.Authorization; // Bổ sung
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;             // Bổ sung
using VoteService.Contracts;
using VoteService.Services;

namespace VoteService.Controllers
{
    [ApiController]
    [Route("api/votes")]
    public class VotesController(IVoteService votes, IConfiguration config) : ControllerBase
    {
        private readonly IVoteService _votes = votes;
        private readonly string _realtimeServiceUrl = config["REALTIME_SERVICE_URL"] ?? "http://pollbuilder-realtimeservice:8080";

        [Authorize] // BẮT BUỘC NGƯỜI VOTE PHẢI ĐĂNG NHẬP
        [HttpPost]
        public async Task<ActionResult<PollResultsDto>> Vote([FromBody] VoteRequest request)
        {
            // 1. Lấy ID của người dùng từ Token (Thay vì dùng Cookie)
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Bạn cần đăng nhập để vote." });
            }

            try
            {
                // 2. Truyền userId (dưới dạng string) vào hàm VoteAsync để lưu lịch sử
                VoteResultDto result = await _votes.VoteAsync(request.PollCode, request.OptionIndex, userId);

                // Gửi thông báo realtime sang RealtimeService nếu là vote mới
                if (result.IsNewVote)
                {
                    try
                    {
                        using var http = new HttpClient();
                        string notifyUrl = $"{_realtimeServiceUrl.TrimEnd('/')}/api/notify/vote";

                        _ = await http.PostAsJsonAsync(notifyUrl, new
                        {
                            Code = request.PollCode,
                            result.Results
                        });
                    }
                    catch
                    {
                        // Nếu RealtimeService chưa phản hồi thì bỏ qua
                    }
                }

                return Ok(result.Results);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Poll not found." });
            }
            catch (InvalidOperationException)
            {
                return Conflict(new { error = "Poll is closed." });
            }
            catch (ArgumentOutOfRangeException)
            {
                return BadRequest(new { error = "Invalid option." });
            }
        }
    }
}
