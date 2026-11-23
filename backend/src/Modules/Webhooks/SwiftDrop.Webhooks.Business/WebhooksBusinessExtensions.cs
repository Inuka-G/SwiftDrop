using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.SharedKernel.Messaging;
using SwiftDrop.Webhooks.Business.Handlers;

namespace SwiftDrop.Webhooks.Business;

public static class WebhooksBusinessExtensions
{
    public static IServiceCollection AddWebhooksBusiness(this IServiceCollection services)
    {
        services.AddScoped<IIntegrationEventHandler<ParcelStatusChanged>, ParcelStatusWebhookHandler>();
        services.AddEventConsumer<ParcelStatusChanged, ParcelStatusWebhookHandler>("q.webhooks");
        return services;
    }
}
