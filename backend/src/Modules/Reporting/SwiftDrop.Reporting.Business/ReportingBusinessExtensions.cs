using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Reporting.Business.Consumers;
using SwiftDrop.Reporting.Business.Services;
using SwiftDrop.Reporting.Contracts;
using SwiftDrop.Reporting.Data;

namespace SwiftDrop.Reporting.Business;

public static class ReportingBusinessExtensions
{
    public static IServiceCollection AddReportingBusiness(this IServiceCollection services, IConfiguration config)
    {
        services.AddReportingData(config);
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IReportDateResolver, ReportDateResolver>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ParcelCreatedConsumer>();
        services.AddScoped<ParcelStatusChangedConsumer>();
        return services;
    }
}
