using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PollService.Contracts;
using PollService.Data;
using PollService.Models;

namespace PollService.Services;

public class PollService : IPollService
{
    private readonly AppDbContext _db;
    private static readonly Random _random = new();

    public PollService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PollDto> CreatePollAsync(string question,
List<string> options)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Question is required.");

        var cleanOptions = options
            .Select(o => o.Trim())
            .Where(o => !string.IsNullOrEmpty(o))
            .ToList();

        if (cleanOptions.Count < 2 || cleanOptions.Count > 6)
            throw new ArgumentException("Options must be between 2 and 6."); 


        var poll = new Poll
        {
            Id = Guid.NewGuid(),
            Code = GenerateCode(),
            Question = question.Trim(),
            OptionsJson = JsonSerializer.Serialize(cleanOptions),
            IsClosed = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Polls.Add(poll);
        await _db.SaveChangesAsync();

        return ToDto(poll);
    }

    public async Task<PollDto?> GetPollAsync(string code)
    {
        var poll = await _db.Polls
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == code);

        return poll is null ? null : ToDto(poll);
    }

    public async Task<PollResultsDto?> GetResultsAsync(string code)
    {
        var poll = await _db.Polls
            .Include(p => p.Votes)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == code);

        return poll is null ? null : ToResults(poll);
    }

    public async Task<PollDto?> ClosePollAsync(string code)
    {
        var poll = await _db.Polls.FirstOrDefaultAsync(p => p.Code ==
code);
        if (poll is null) return null;

        poll.IsClosed = true;
        await _db.SaveChangesAsync();

        return ToDto(poll);
    }

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Range(0, 6)
            .Select(_ => chars[_random.Next(chars.Length)])
            .ToArray());
    }

    private static List<string> GetOptions(Poll poll)
    {
        return JsonSerializer.Deserialize<List<string>>(poll.OptionsJson)
?? new();
    }

    private static PollDto ToDto(Poll poll)
    {
        var options = GetOptions(poll);
        return new PollDto(
            poll.Code,
            poll.Question,
            options.Select((text, i) => new PollOptionDto(i,
text)).ToList(),
            poll.IsClosed ? "closed" : "open"
        );
    }

    private static PollResultsDto ToResults(Poll poll)
    {
        var options = GetOptions(poll);
        var counts = options
            .Select((_, index) => poll.Votes.Count(v => v.OptionIndex ==
index))
            .ToList();

        return new PollResultsDto(
            poll.Code,
            poll.Question,
            options.Select((text, i) => new PollOptionDto(i,
text)).ToList(),
            counts,
            counts.Sum(),
            poll.IsClosed ? "closed" : "open"
        );
    }
}