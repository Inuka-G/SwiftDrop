using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Dispatch.Api.Dtos;
using SwiftDrop.Dispatch.Business;
using SwiftDrop.Dispatch.Data;

namespace SwiftDrop.Dispatch.Api;

public static class DispatchModule
{
    public static IServiceCollection AddDispatchModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddControllers().AddApplicationPart(typeof(DispatchModule).Assembly);
        services.AddScoped<IValidator<ListUnassignedRequest>, ListUnassignedValidator>();
        services.AddScoped<IValidator<AssignCourierRequest>, AssignCourierValidator>();
        services.AddScoped<IValidator<RegisterCourierRequest>, RegisterCourierValidator>();
        services.AddScoped<IValidator<UpdateCourierRequest>, UpdateCourierValidator>();
        services.AddScoped<IValidator<ListCouriersRequest>, ListCouriersValidator>();
        services.AddDispatchBusiness(config);
        return services;
    }

    public static Task InitializeDispatchModuleAsync(this WebApplication app) =>
        app.Services.InitializeDispatchDataAsync();
}
