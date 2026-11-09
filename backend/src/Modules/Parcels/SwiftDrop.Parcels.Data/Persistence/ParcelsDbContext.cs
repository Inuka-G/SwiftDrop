using Microsoft.EntityFrameworkCore;
using SwiftDrop.Parcels.Data.Entities;

namespace SwiftDrop.Parcels.Data.Persistence;

public sealed class ParcelsDbContext(DbContextOptions<ParcelsDbContext> options) : DbContext(options)
{
    public DbSet<Parcel> Parcels => Set<Parcel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (Database.IsNpgsql())
            modelBuilder.HasDefaultSchema("parcels");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ParcelsDbContext).Assembly);
    }
}