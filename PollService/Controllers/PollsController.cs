using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VoteService.Data;
using VoteService.Models;

namespace VoteService.Controllers
{
    [ApiController]
    [Route("api/internal/polls")]
    public class InternalController : ControllerBase
    {
        private readonly AppDbContext _db;

        public InternalController(AppDbContext db)
        {
            _db = db;
        }

        public class SyncPollRequest
        {
            public string Code { get; set; } = "";
            public string Question { get; set; } = "";
            public List<string> Options { get; set; } = new();
        }

        [HttpPost]
        public async Task<IActionResult> SyncPoll([FromBody] SyncPollRequest request)
        {
            var exists = await _db.Polls.AnyAsync(p => p.Code == request.Code);
            if (exists)
            {
                return Ok(new { message = "Poll already exists." });
            }

            var poll = new Poll
            {
                Id = Guid.NewGuid(),
                Code = request.Code,
                Question = request.Question,
                OptionsJson = JsonSerializer.Serialize(request.Options),
                IsClosed = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.Polls.Add(poll);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Poll synced.", code = poll.Code });
        }
    }
}
