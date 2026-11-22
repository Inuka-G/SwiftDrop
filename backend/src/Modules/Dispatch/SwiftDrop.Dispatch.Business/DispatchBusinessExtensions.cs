using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Dispatch.Business.Services;
using SwiftDrop.Dispatch.Business.Consumers;
using SwiftDrop.Dispatch.Contracts;
using SwiftDrop.Dispatch.Data;

namespace SwiftDrop.Dispatch.Business;

public static class DispatchBusinessExtensions
{
    public static IServiceCollection AddDispatchBusiness(this IServiceCollection services, IConfiguration config)
    {
        services.AddDispatchData(config);
        services.AddScoped<IDispatchService, DispatchService>();
        services.AddScoped<ParcelCreatedConsumer>();
        services.AddScoped<ParcelStatusChangedConsumer>();
        services.AddScoped<UserRoleChangedConsumer>();
        services.AddScoped<UserStatusChangedConsumer>();
        services.AddScoped<UnconfiguredDispatchAdapters>();
        services.AddScoped<ICourierEligibilityGateway>(sp => sp.GetRequiredService<UnconfiguredDispatchAdapters>());
        services.AddScoped<IParcelAssignmentGateway, ParcelAssignmentGateway>();
        services.AddScoped<IDispatchEventPublisher>(sp => sp.GetRequiredService<UnconfiguredDispatchAdapters>());
        services.AddScoped<IZoneLookup>(sp => sp.GetRequiredService<UnconfiguredDispatchAdapters>());
        return services;
    }
}
