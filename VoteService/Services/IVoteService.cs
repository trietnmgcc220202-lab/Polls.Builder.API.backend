using VoteService.Contracts;

namespace VoteService.Services;

public interface IVoteService
{
    Task<VoteResultDto> VoteAsync(string code, int optionIndex, string
voterToken);
}