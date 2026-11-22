using Microsoft.EntityFrameworkCore;
using SwiftDrop.Pricing.Data.Entities;

namespace SwiftDrop.Pricing.Data.Persistence;

public class PricingDbContext : DbContext
{
    public PricingDbContext(DbContextOptions<PricingDbContext> options) : base(options) { }

    protected PricingDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<PricingRule> PricingRules => Set<PricingRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Postgres: schema per module. Sqlite has no schemas, so it is skipped there.
        if (Database.IsNpgsql())
            modelBuilder.HasDefaultSchema("pricing");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PricingDbContext).Assembly);
    }
}
