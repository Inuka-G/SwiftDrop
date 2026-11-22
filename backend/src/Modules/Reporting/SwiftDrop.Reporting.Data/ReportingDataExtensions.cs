using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Reporting.Data.Persistence;
using SwiftDrop.Reporting.Data.Repositories;
using SwiftDrop.SharedKernel.Persistence;

namespace SwiftDrop.Reporting.Data;

public static class ReportingDataExtensions
{
    public static IServiceCollection AddReportingData(this IServiceCollection services, IConfiguration config)
    {
        services.AddModuleDbContext<ReportingDbContext>(config, "Reporting");
        services.AddScoped<IReportingRepository, ReportingRepository>();
        return services;
    }

    public static async Task InitializeReportingDataAsync(this IServiceProvider services, bool seedSampleData,
        CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
        await db.Database.EnsureCreatedAsync(ct);

        if (!db.Database.IsNpgsql())
        {
            var columns = await db.Database.SqlQuery<string>(
                $"SELECT name AS \"Value\" FROM pragma_table_info('daily_statistics')")
                .ToListAsync(ct);

            if (columns.Contains("ReportDate", StringComparer.OrdinalIgnoreCase)
                && !columns.Contains("Date", StringComparer.OrdinalIgnoreCase))
            {
                await db.Database.ExecuteSqlRawAsync(
                    """ALTER TABLE "daily_statistics" RENAME COLUMN "ReportDate" TO "Date" """,
                    ct);
                columns.Remove("ReportDate");
                columns.Add("Date");
            }

            if (!columns.Contains("UpdatedAt", StringComparer.OrdinalIgnoreCase))
            {
                await db.Database.ExecuteSqlRawAsync(
                    """ALTER TABLE "daily_statistics" ADD COLUMN "UpdatedAt" INTEGER NOT NULL DEFAULT 0 """,
                    ct);
            }
        }

        if (seedSampleData) await ReportingDbSeeder.SeedSampleDataAsync(db, ct);
    }
}
