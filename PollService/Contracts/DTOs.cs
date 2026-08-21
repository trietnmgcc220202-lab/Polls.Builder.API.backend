using System.ComponentModel.DataAnnotations;

namespace PollService.Contracts;

public class CreatePollRequest
{
    [Required]
    public string Question { get; set; } = "";

    [Required]
    public List<string> Options { get; set; } = new();
}

public record PollOptionDto(int Index, string Text);

public record PollDto(
    string Code,
    string Question,
    List<PollOptionDto> Options,
    string Status
);

public record PollResultsDto(
    string Code,
    string Question,
    List<PollOptionDto> Options,
    List<int> Counts,
    int Total,
    string Status
);