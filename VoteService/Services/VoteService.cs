using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VoteService.Contracts;
using VoteService.Data;
using VoteService.Models;

namespace VoteService.Services;

public class VoteService : IVoteService
{
    private readonly AppDbContext _db;

    public VoteService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<VoteResultDto> VoteAsync(string code, int optionIndex, string voterToken)
    {
        var cleanCode = code.Trim();
        var poll = await _db.Polls
            .Include(p => p.Votes)
            .FirstOrDefaultAsync(p => p.Code.ToLower() == cleanCode.ToLower());

        if (poll is null)
            throw new KeyNotFoundException("Poll not found.");

        if (poll.IsClosed)
            throw new InvalidOperationException("Poll is closed.");

        var options = JsonSerializer.Deserialize<List<string>>(poll.OptionsJson) ?? new();

        if (optionIndex < 0 || optionIndex >= options.Count)
            throw new ArgumentOutOfRangeException(nameof(optionIndex), "Invalid option.");

        var alreadyVoted = poll.Votes.Any(v => v.VoterToken == voterToken);
        if (alreadyVoted)
        {
            return new VoteResultDto(false, ToResults(poll, options));
        }

        var vote = new Vote
        {
            Id = Guid.NewGuid(),
            PollId = poll.Id,
            OptionIndex = optionIndex,
            VoterToken = voterToken,
            CreatedAt = DateTime.UtcNow
        };

        _db.Votes.Add(vote);
        await _db.SaveChangesAsync();

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
            options.Select((text, i) => new PollOptionDto(i, text)).ToList(),
            counts,
            counts.Sum(),
            poll.IsClosed ? "closed" : "open"
        );
    }
}
