using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SwiftDrop.Parcels.Data.Persistence;

public sealed class ParcelsDbContextFactory : IDesignTimeDbContextFactory<ParcelsDbContext>
{
    public ParcelsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ParcelsDbContext>();

        optionsBuilder.UseNpgsql(
            Environment.GetEnvironmentVariable("ConnectionStrings__Postgres") ?? "Host=localhost;Port=5433;Database=swiftdrop;Username=postgres;Password=postgres",
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "parcels"));

        return new ParcelsDbContext(optionsBuilder.Options);
    }
}
