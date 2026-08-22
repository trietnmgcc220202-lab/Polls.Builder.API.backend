using AccountService.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Data;

public class AccountDbContext : DbContext
{
    public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options) { }
    public DbSet<User> Users { get; set; }
}