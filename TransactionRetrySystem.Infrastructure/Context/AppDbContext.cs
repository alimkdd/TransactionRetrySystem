using Microsoft.EntityFrameworkCore;
using TransactionRetrySystem.Domain.Models;

namespace TransactionRetrySystem.Infrastructure.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

    public DbSet<Status> Statuses { get; set; }
    public DbSet<ErrorType> ErrorTypes { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<TransactionAttempt> TransactionAttempts { get; set; }
    public DbSet<RetryQueue> RetryQueue { get; set; }
    public DbSet<CircuitBreakerState> CircuitBreakerStates { get; set; }
}