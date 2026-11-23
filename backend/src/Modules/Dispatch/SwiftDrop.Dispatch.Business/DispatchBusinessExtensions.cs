using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Dispatch.Business.Services;
using SwiftDrop.Dispatch.Business.Consumers;
using SwiftDrop.Dispatch.Contracts;
using SwiftDrop.Dispatch.Data;
using SwiftDrop.SharedKernel.Messaging;
using SwiftDrop.Identity.Contracts;

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
        services.AddScoped<ICourierEligibilityGateway, IdentityCourierEligibilityAdapter>();
        services.AddScoped<IIdentityEventPublisher, DispatchIdentityEventPublisher>();
        services.AddScoped<IParcelAssignmentGateway, ParcelAssignmentGateway>();
        services.AddScoped<IZoneLookup, PricingZoneLookup>();
        services.AddHostedService<DispatchOutboxProcessor>();

        services.AddScoped<IIntegrationEventHandler<SwiftDrop.Parcels.Contracts.Events.ParcelCreatedIntegrationEvent>, ParcelCreatedEventHandler>();
        services.AddScoped<IIntegrationEventHandler<SwiftDrop.Parcels.Contracts.Events.ParcelStatusChangedIntegrationEvent>, ParcelStatusChangedEventHandler>();
        services.AddScoped<IIntegrationEventHandler<SwiftDrop.Identity.Contracts.UserRoleChanged>, IdentityUserRoleChangedEventHandler>();
        services.AddScoped<IIntegrationEventHandler<SwiftDrop.Identity.Contracts.UserStatusChanged>, IdentityUserStatusChangedEventHandler>();
        services.AddEventConsumer<SwiftDrop.Parcels.Contracts.Events.ParcelCreatedIntegrationEvent>(
            "q.dispatch.parcel-created", SwiftDrop.Parcels.Contracts.Events.ParcelEventRoutingKeys.Created);
        services.AddEventConsumer<SwiftDrop.Parcels.Contracts.Events.ParcelStatusChangedIntegrationEvent>(
            "q.dispatch.parcel-status", SwiftDrop.Parcels.Contracts.Events.ParcelEventRoutingKeys.StatusChanged);
        services.AddEventConsumer<SwiftDrop.Identity.Contracts.UserRoleChanged>("q.dispatch.identity-role", "identity.user-role-changed");
        services.AddEventConsumer<SwiftDrop.Identity.Contracts.UserStatusChanged>("q.dispatch.identity-status", "identity.user-status-changed");
        services.PostConfigure<RabbitMqOptions>(options =>
            options.RequiredOutboxRoutes = [.. options.RequiredOutboxRoutes,
                "identity.user-role-changed", "identity.user-status-changed"]);
        return services;
    }
}
