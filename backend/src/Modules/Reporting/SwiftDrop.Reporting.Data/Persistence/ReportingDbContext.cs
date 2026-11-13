using Microsoft.EntityFrameworkCore;
using SwiftDrop.Reporting.Data.Entities;

namespace SwiftDrop.Reporting.Data.Persistence;

internal sealed class ReportingDbContext(DbContextOptions<ReportingDbContext> options) : DbContext(options)
{
    internal DbSet<DailyStatistic> DailyStatistics => Set<DailyStatistic>();
    internal DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (Database.IsNpgsql()) modelBuilder.HasDefaultSchema("reporting");
        modelBuilder.Entity<DailyStatistic>(e =>
        {
            e.ToTable("daily_statistics");
            e.HasKey(x => x.Date);
            e.Property(x => x.Date).ValueGeneratedNever();
            e.Property(x => x.UpdatedAt).HasConversion(
                value => value.ToUnixTimeMilliseconds(),
                value => DateTimeOffset.FromUnixTimeMilliseconds(value));
        });
        modelBuilder.Entity<ProcessedEvent>(e =>
        {
            e.ToTable("processed_events");
            e.HasKey(x => x.EventId);
            e.Property(x => x.ProcessedAt).HasConversion(
                value => value.ToUnixTimeMilliseconds(),
                value => DateTimeOffset.FromUnixTimeMilliseconds(value));
        });
    }
}
