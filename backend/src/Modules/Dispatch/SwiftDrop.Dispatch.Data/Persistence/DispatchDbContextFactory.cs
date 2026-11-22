using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SwiftDrop.Dispatch.Data.Persistence;

internal sealed class DispatchDbContextFactory : IDesignTimeDbContextFactory<DispatchDbContext>
{
    public DispatchDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DispatchDbContext>();

        optionsBuilder.UseNpgsql(
            Environment.GetEnvironmentVariable("ConnectionStrings__Postgres") ?? "Host=localhost;Port=5433;Database=swiftdrop;Username=postgres;Password=postgres",
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "dispatch"));

        return new DispatchDbContext(optionsBuilder.Options);
    }
}
