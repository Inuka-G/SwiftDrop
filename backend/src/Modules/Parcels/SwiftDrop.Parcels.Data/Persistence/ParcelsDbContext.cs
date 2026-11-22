using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SwiftDrop.Parcels.Data.Entities;

namespace SwiftDrop.Parcels.Data.Persistence;

public sealed class ParcelsDbContext(DbContextOptions<ParcelsDbContext> options) : DbContext(options)
{
    public DbSet<Parcel> Parcels => Set<Parcel>();
    public DbSet<ParcelStatusHistory> StatusHistory => Set<ParcelStatusHistory>();
    public DbSet<DeliveryProof> DeliveryProofs => Set<DeliveryProof>();
    public DbSet<FailureReport> FailureReports => Set<FailureReport>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (Database.IsNpgsql())
            modelBuilder.HasDefaultSchema("parcels");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ParcelsDbContext).Assembly);
    }

    // Every time in this module is UTC. SQLite stores no time zone, so without this the
    // values come back as "Unspecified", are sent to browsers without a "Z", and are
    // shown as local time (5.5 hours early in Sri Lanka).
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
}