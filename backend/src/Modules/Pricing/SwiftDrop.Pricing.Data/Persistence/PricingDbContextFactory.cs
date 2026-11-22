using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SwiftDrop.Pricing.Data.Persistence;

public sealed class PricingDbContextFactory : IDesignTimeDbContextFactory<PricingDbContext>
{
    public PricingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PricingDbContext>();

        optionsBuilder.UseNpgsql(
            Environment.GetEnvironmentVariable("ConnectionStrings__Postgres") ?? "Host=localhost;Port=5433;Database=swiftdrop;Username=postgres;Password=postgres",
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "pricing"));

        return new PricingDbContext(optionsBuilder.Options);
    }
}
