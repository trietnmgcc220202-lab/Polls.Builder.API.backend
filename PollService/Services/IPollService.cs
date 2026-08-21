using PollService.Contracts;

namespace PollService.Services;

public interface IPollService
{
    Task<PollDto> CreatePollAsync(string question, List<string> options);
    Task<PollDto?> GetPollAsync(string code);
    Task<PollResultsDto?> GetResultsAsync(string code);
    Task<PollDto?> ClosePollAsync(string code);
}