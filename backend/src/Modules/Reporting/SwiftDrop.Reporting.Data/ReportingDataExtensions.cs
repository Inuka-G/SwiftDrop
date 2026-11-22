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
        if (db.Database.IsNpgsql())
            await InitializePostgresTablesAsync(db, ct);
        else
            await db.Database.EnsureCreatedAsync(ct);
        if (seedSampleData) await ReportingDbSeeder.SeedSampleDataAsync(db, ct);
    }

    private static async Task InitializePostgresTablesAsync(ReportingDbContext db, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(286540611, 3)", ct);
        var count = await db.Database.SqlQuery<int>($"""
            SELECT count(*)::int AS "Value" FROM information_schema.tables
            WHERE table_schema = 'reporting' AND table_type = 'BASE TABLE'
                AND table_name IN ('daily_statistics', 'processed_events')
            """).SingleAsync(ct);
        if (count == 0)
            await db.Database.ExecuteSqlRawAsync(db.Database.GenerateCreateScript(), ct);
        else if (count != 2)
            throw new InvalidOperationException("The Reporting schema is incomplete. Restore the missing table before startup.");

        await transaction.CommitAsync(ct);
    }
}
