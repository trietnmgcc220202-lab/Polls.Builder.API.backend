namespace PollService.Models;

public class Poll
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Question { get; set; } = "";
    public string OptionsJson { get; set; } = "[]";
    public bool IsClosed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Vote> Votes { get; set; } = new();
}