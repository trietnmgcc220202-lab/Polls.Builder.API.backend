using System.ComponentModel.DataAnnotations;

namespace VoteService.Contracts;

public class VoteRequest
{
    [Range(0, 100)]
    public int OptionIndex { get; set; }
}

public record PollOptionDto(int Index, string Text);

public record PollResultsDto(
    string Code,
    string Question,
    List<PollOptionDto> Options,
    List<int> Counts,
    int Total,
    string Status
);

public record VoteResultDto(
    bool IsNewVote,
    PollResultsDto Results
);