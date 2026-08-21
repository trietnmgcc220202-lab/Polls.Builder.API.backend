using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using VoteService.Contracts;
using VoteService.Data;
using VoteService.Services;

namespace VoteService.Controllers
{
    [ApiController]
    [Route("api/polls")]
    public class VotesController : ControllerBase
    {
        private readonly IVoteService _votes;
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private const string VoterCookie = "voter_token";

        public VotesController(IVoteService votes, AppDbContext db, IConfiguration config)
        {
            _votes = votes;
            _db = db;
            _config = config;
        }

        [HttpPost("{code}/vote")]
        public async Task<IActionResult> Vote(string code, [FromBody] VoteRequest request)
        {
            var token = GetOrCreateVoterToken();

            try
            {
                var result = await _votes.VoteAsync(code, request.OptionIndex, token);

                if (result.IsNewVote)
                {
                    try
                    {
                        var realtimeUrl = _config["RealtimeServiceUrl"] ?? "https://pollbuilder-realtimeservice.onrender.com";
                        using var http = new HttpClient();
                        await http.PostAsJsonAsync($"{realtimeUrl}/api/notify/vote", new
                        {
                            Code = code,
                            Results = result.Results
                        });
                    }
                    catch { }
                }

                return Ok(result.Results);
            }
            catch (KeyNotFoundException)
            {
                // Bắt bệnh DB: Trả về số lượng Poll đang có trong DB của VoteService
                var totalPolls = await _db.Polls.CountAsync();
                var sampleCodes = await _db.Polls.Select(p => p.Code).Take(5).ToListAsync();

                return NotFound(new
                {
                    error = "Poll not found in VoteService Database.",
                    searchingCode = code,
                    totalPollsInVoteDb = totalPolls,
                    availableCodesInVoteDb = sampleCodes
                });
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
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

            return token;
        }
    }
}
