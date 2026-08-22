namespace VoteService.Models
{
    public class Vote
    {
        public Guid Id { get; set; }
        public Guid PollId { get; set; }
        public int OptionIndex { get; set; }
        public string VoterToken { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Poll? Poll { get; set; }
    }
}
