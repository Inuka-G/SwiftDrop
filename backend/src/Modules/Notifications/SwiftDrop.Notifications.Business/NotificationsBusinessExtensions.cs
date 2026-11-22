using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SwiftDrop.Notifications.Business.Email;
using SwiftDrop.Notifications.Data;
using SwiftDrop.Notifications.Business.Handlers;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.Notifications.Business;

public static class NotificationsBusinessExtensions
{
    public static IServiceCollection AddNotificationsBusiness(this IServiceCollection services, IConfiguration config)
    {
        services.AddNotificationsData(config);
        services.AddOptions<NotificationEmailOptions>()
            .Bind(config.GetSection(NotificationEmailOptions.SectionName))
            .Validate(options => options.IsValid(), "Invalid notification SMTP settings.")
            .ValidateOnStart();
        services.AddScoped<INotificationEmailSender, SmtpNotificationEmailSender>();
        services.AddScoped<IIntegrationEventHandler<ParcelStatusChangedIntegrationEvent>, ParcelStatusNotificationHandler>();
        services.AddEventConsumer<ParcelStatusChangedIntegrationEvent>("q.notifications",
            ParcelEventRoutingKeys.StatusChanged, previousRoutingKey: "parcel.status.changed");
        return services;
    }

    public static async Task InitializeNotificationsAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        if (scope.ServiceProvider.GetRequiredService<IOptions<NotificationEmailOptions>>().Value.Enabled)
            await scope.ServiceProvider.GetRequiredService<NotificationDeliveryStore>().InitializeAsync(ct);
    }
}
