using Microsoft.EntityFrameworkCore;
using SwiftDrop.Pricing.Data.Entities;

namespace SwiftDrop.Pricing.Data.Persistence;

internal sealed class PricingDbContext(DbContextOptions<PricingDbContext> options) : DbContext(options)
{
    public DbSet<Zone> Zones => Set<Zone>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Postgres: schema per module. Sqlite has no schemas, so it is skipped there.
        if (Database.IsNpgsql())
            modelBuilder.HasDefaultSchema("pricing");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PricingDbContext).Assembly);
    }
}