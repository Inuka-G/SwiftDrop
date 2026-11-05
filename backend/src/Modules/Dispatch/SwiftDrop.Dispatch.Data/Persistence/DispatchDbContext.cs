using Microsoft.EntityFrameworkCore;
using SwiftDrop.Dispatch.Data.Entities;

namespace SwiftDrop.Dispatch.Data.Persistence;

internal sealed class DispatchDbContext(DbContextOptions<DispatchDbContext> options) : DbContext(options)
{
    internal DbSet<ParcelAssignment> Assignments => Set<ParcelAssignment>();
    internal DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (Database.IsNpgsql()) modelBuilder.HasDefaultSchema("dispatch");
        modelBuilder.Entity<ParcelAssignment>(e =>
        {
            e.ToTable("parcel_assignments");
            e.HasKey(x => x.ParcelId);
            e.Property(x => x.TrackingNumber).HasMaxLength(80).IsRequired();
            e.Property(x => x.State).HasMaxLength(24).IsRequired();
            // Store timestamps as epoch milliseconds so SQLite can filter and sort them.
            e.Property(x => x.CreatedAt).HasConversion(
                value => value.ToUnixTimeMilliseconds(), value => DateTimeOffset.FromUnixTimeMilliseconds(value));
            e.Property(x => x.AssignedAt).HasConversion(
                value => value.HasValue ? value.Value.ToUnixTimeMilliseconds() : (long?)null,
                value => value.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(value.Value) : (DateTimeOffset?)null);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.State, x.ZoneId, x.CreatedAt });
        });
        modelBuilder.Entity<ProcessedEvent>(e =>
        {
            e.ToTable("processed_events");
            e.HasKey(x => x.EventId);
        });
    }
}
