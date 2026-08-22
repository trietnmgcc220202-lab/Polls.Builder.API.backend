using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VoteService.Contracts;
using VoteService.Data;
using VoteService.Models;

namespace VoteService.Services
{
    public class VoteService(AppDbContext db) : IVoteService
    {
        private readonly AppDbContext _db = db;

        public async Task<VoteResultDto> VoteAsync(string code, int optionIndex, string voterToken)
        {
            Poll? poll = await _db.Polls
                .Include(p => p.Votes)
                .FirstOrDefaultAsync(p => p.Code == code);

            
            Console.WriteLine($">>> [VoteService] Code={code}, Found={poll != null}, IsClosed={poll?.IsClosed}, VotesCount={poll?.Votes?.Count}");
            // =============================

            if (poll is null)
            {
                throw new KeyNotFoundException("Poll not found.");
            }

            if (poll.IsClosed)
            {
                throw new InvalidOperationException("Poll is closed.");
            }

            List<string> options = JsonSerializer.Deserialize<List<string>>(poll.OptionsJson) ?? [];

            if (optionIndex < 0 || optionIndex >= options.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(optionIndex), "Invalid option.");
            }

            // Kiểm tra đã vote chưa 
            bool alreadyVoted = poll.Votes.Any(v => v.VoterToken == voterToken);
            if (alreadyVoted)
            {
                // Trả về kết quả hiện tại, không tạo vote mới 
                return new VoteResultDto(false, ToResults(poll, options));
            }

            // Tạo vote mới 
            var vote = new Vote
            {
                Id = Guid.NewGuid(),
                PollId = poll.Id,
                OptionIndex = optionIndex,
                VoterToken = voterToken,
                CreatedAt = DateTime.UtcNow
            };

            _ = _db.Votes.Add(vote);
            _ = await _db.SaveChangesAsync();

            // Reload votes để đếm chính xác 
            await _db.Entry(poll).Collection(p => p.Votes).LoadAsync();

            return new VoteResultDto(true, ToResults(poll, options));
        }

        private static PollResultsDto ToResults(Poll poll, List<string> options)
        {
            var counts = options
                .Select((_, index) => poll.Votes.Count(v => v.OptionIndex == index))
                .ToList();

            return new PollResultsDto(
                poll.Code,
                poll.Question,
                [.. options.Select((text, i) => new PollOptionDto(i, text))],
                counts,
                counts.Sum(),
                poll.IsClosed ? "closed" : "open"
            );
        }
    }
}
