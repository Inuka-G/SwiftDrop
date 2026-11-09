using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Reporting.Api.Dtos;
using SwiftDrop.Reporting.Business;
using SwiftDrop.Reporting.Data;

namespace SwiftDrop.Reporting.Api;

public static class ReportingModule
{
    public static IServiceCollection AddReportingModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddControllers().AddApplicationPart(typeof(ReportingModule).Assembly);
        services.AddScoped<IValidator<ReportRangeRequest>, ReportRangeRequestValidator>();
        services.AddReportingBusiness(config);
        return services;
    }

    public static Task InitializeReportingModuleAsync(this WebApplication app) =>
        app.Services.InitializeReportingDataAsync(app.Environment.IsDevelopment());
}
