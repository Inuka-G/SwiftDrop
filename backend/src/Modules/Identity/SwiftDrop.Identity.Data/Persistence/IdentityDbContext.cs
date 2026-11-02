using Microsoft.EntityFrameworkCore;
using SwiftDrop.Identity.Data.Entities;

namespace SwiftDrop.Identity.Data.Persistence;

internal sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Postgres: schema per module. Sqlite has no schemas, so it is skipped there.
        if (Database.IsNpgsql())
            modelBuilder.HasDefaultSchema("identity");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
