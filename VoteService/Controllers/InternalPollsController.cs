using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoteService.Data;
using VoteService.Models;

namespace VoteService.Controllers
{
    [ApiController]
    [Route("api/internal/polls")]
    public class InternalPollsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public InternalPollsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> CreateInternalPoll([FromBody] CreateInternalPollDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
            {
                return BadRequest("Code is required.");
            }

            var existing = await _db.Polls.FirstOrDefaultAsync(p => p.Code.ToLower() == dto.Code.ToLower());
            if (existing != null)
            {
                return Ok(existing);
            }

            var poll = new Poll
            {
                Id = Guid.NewGuid(),
                Code = dto.Code,
                Question = dto.Question,
                OptionsJson = JsonSerializer.Serialize(dto.Options),
                IsClosed = false
            };

            _db.Polls.Add(poll);
            await _db.SaveChangesAsync();

            return Ok(poll);
        }
    }

    public class CreateInternalPollDto
    {
        public string Code { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
    }
}
