using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SwiftDrop.Identity.Data.Persistence;

internal sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();

        optionsBuilder.UseNpgsql(
            Environment.GetEnvironmentVariable("ConnectionStrings__Postgres") ?? "Host=localhost;Port=5433;Database=swiftdrop;Username=postgres;Password=postgres",
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "identity"));

        return new IdentityDbContext(optionsBuilder.Options);
    }
}
