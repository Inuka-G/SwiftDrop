using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Notifications.Business.Handlers;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.Notifications.Business;

public static class NotificationsBusinessExtensions
{
    public static IServiceCollection AddNotificationsBusiness(this IServiceCollection services)
    {
        services.AddScoped<IIntegrationEventHandler<ParcelStatusChangedIntegrationEvent>, ParcelStatusNotificationHandler>();
        services.AddEventConsumer<ParcelStatusChangedIntegrationEvent>("q.notifications",
            ParcelEventRoutingKeys.StatusChanged, previousRoutingKey: "parcel.status.changed");
        return services;
    }
}
