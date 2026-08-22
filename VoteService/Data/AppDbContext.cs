using Microsoft.EntityFrameworkCore;
using VoteService.Models;

namespace VoteService.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Poll> Polls => Set<Poll>();
        public DbSet<Vote> Votes => Set<Vote>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.Entity<Poll>(e =>
            {
                _ = e.HasKey(x => x.Id);
                _ = e.HasIndex(x => x.Code).IsUnique();
                _ = e.Property(x => x.Code).HasMaxLength(12);
                _ = e.Property(x => x.Question).HasMaxLength(500);
            });

            _ = modelBuilder.Entity<Vote>(e =>
            {
                _ = e.HasKey(x => x.Id);
                _ = e.HasIndex(x => new { x.PollId, x.VoterToken }).IsUnique();
                _ = e.HasOne(x => x.Poll)
                 .WithMany(p => p.Votes)
                 .HasForeignKey(x => x.PollId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
