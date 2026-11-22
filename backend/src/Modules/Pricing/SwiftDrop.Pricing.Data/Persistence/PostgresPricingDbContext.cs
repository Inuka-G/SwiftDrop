using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SwiftDrop.Pricing.Data.Persistence;

internal sealed class PostgresPricingDbContext(DbContextOptions<PostgresPricingDbContext> options)
    : PricingDbContext(options);

internal sealed class PostgresPricingDesignTimeFactory : IDesignTimeDbContextFactory<PostgresPricingDbContext>
{
    public PostgresPricingDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<PostgresPricingDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
                ?? "Host=localhost;Database=swiftdrop", options =>
                options.MigrationsHistoryTable("__EFMigrationsHistory", "pricing"))
            .Options);
}
