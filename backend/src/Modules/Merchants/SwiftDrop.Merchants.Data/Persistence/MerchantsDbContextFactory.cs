using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SwiftDrop.Merchants.Data.Persistence;

internal sealed class MerchantsDbContextFactory : IDesignTimeDbContextFactory<MerchantsDbContext>
{
    public MerchantsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MerchantsDbContext>();

        optionsBuilder.UseNpgsql(
            Environment.GetEnvironmentVariable("ConnectionStrings__Postgres") ?? "Host=localhost;Port=5433;Database=swiftdrop;Username=postgres;Password=postgres",
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "merchants"));

        return new MerchantsDbContext(optionsBuilder.Options);
    }
}
