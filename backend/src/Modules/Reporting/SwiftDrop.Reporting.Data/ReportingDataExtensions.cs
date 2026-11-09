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
        if (seedSampleData) await ReportingDbSeeder.SeedSampleDataAsync(db, ct);
    }
}
