using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using VoteService.Contracts;
using VoteService.Services;

namespace VoteService.Controllers
{
    [ApiController]
    [Route("api/polls")]
    public class VotesController : ControllerBase
    {
        private readonly IVoteService _votes;
        private const string VoterCookie = "voter_token";

        public VotesController(IVoteService votes)
        {
            _votes = votes;
        }

        [HttpPost("{code}/vote")]
        public async Task<ActionResult<PollResultsDto>> Vote(string code, [FromBody] VoteRequest request)
        {
            var token = GetOrCreateVoterToken();

            try
            {
                var result = await _votes.VoteAsync(code, request.OptionIndex, token);

                // Gửi thông báo realtime sang RealtimeService nếu là vote mới
                if (result.IsNewVote)
                {
                    try
                    {
                        using var http = new HttpClient();
                        await http.PostAsJsonAsync("http://localhost:5003/api/notify/vote", new
                        {
                            Code = code,
                            Results = result.Results
                        });
                    }
                    catch
                    {
                        // Nếu RealtimeService chưa chạy thì bỏ qua, không làm fail tiến trình vote
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
            if (Request.Cookies.TryGetValue(VoterCookie, out var token) && !string.IsNullOrEmpty(token))
            {
                return token;
            }

            token = Guid.NewGuid().ToString("N");

            Response.Cookies.Append(VoterCookie, token, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

            return token;
        }
    }
}