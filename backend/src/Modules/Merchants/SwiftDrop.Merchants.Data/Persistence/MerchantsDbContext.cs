using Microsoft.EntityFrameworkCore;
using SwiftDrop.Merchants.Data.Entities;

namespace SwiftDrop.Merchants.Data.Persistence;

internal sealed class MerchantsDbContext(DbContextOptions<MerchantsDbContext> options) : DbContext(options)
{
    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (Database.IsNpgsql())
            modelBuilder.HasDefaultSchema("merchants");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MerchantsDbContext).Assembly);
    }
}

