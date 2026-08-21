using Microsoft.EntityFrameworkCore;
using PollService.Models;

namespace PollService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) :
base(options)
    { }

    public DbSet<Poll> Polls => Set<Poll>();
    public DbSet<Vote> Votes => Set<Vote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Poll>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(12);
            e.Property(x => x.Question).HasMaxLength(500);
        });

        modelBuilder.Entity<Vote>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.PollId, x.VoterToken }).IsUnique();
            e.HasOne(x => x.Poll)
             .WithMany(p => p.Votes)
             .HasForeignKey(x => x.PollId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}