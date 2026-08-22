using Microsoft.AspNetCore.Mvc;
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
        private const string VoterCookie = "voter_token";

        [HttpPost]
        public async Task<ActionResult<PollResultsDto>> Vote([FromBody] VoteRequest request)
        {
            string token = GetOrCreateVoterToken();

            try
            {
                // Sử dụng request.PollCode từ gói dữ liệu JSON Frontend gửi lên
                VoteResultDto result = await _votes.VoteAsync(request.PollCode, request.OptionIndex, token);

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
                        // Nếu RealtimeService chưa phản hồi thì bỏ qua, đảm bảo tiến trình vote luôn thành công
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

        private string GetOrCreateVoterToken()
        {
            if (Request.Cookies.TryGetValue(VoterCookie, out string? token) && !string.IsNullOrEmpty(token))
            {
                return token;
            }

            token = Guid.NewGuid().ToString("N");

            Response.Cookies.Append(VoterCookie, token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Bật Secure = true để hoạt động chuẩn HTTPS trên Render
                SameSite = SameSiteMode.None, 
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

            return token;
        }
    }
}
