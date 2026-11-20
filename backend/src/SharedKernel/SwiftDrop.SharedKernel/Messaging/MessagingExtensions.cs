using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace SwiftDrop.SharedKernel.Messaging;

public static class MessagingExtensions
{
    /// Registers a publisher that only logs, so modules can publish before the RabbitMQ
    /// publisher exists. TryAdd means a real publisher registered anywhere else wins.
    public static IServiceCollection AddIntegrationEventPublisherFallback(this IServiceCollection services)
    {
        services.TryAddSingleton<IIntegrationEventPublisher, LoggingIntegrationEventPublisher>();
        return services;
    }
}

internal sealed class LoggingIntegrationEventPublisher(ILogger<LoggingIntegrationEventPublisher> logger)
    : IIntegrationEventPublisher
{
    // Only the id and routing key are logged: payloads may identify customers.
    public Task PublishAsync(Guid eventId, string routingKey, string payloadJson, CancellationToken ct)
    {
        logger.LogInformation("No message broker configured. Event {EventId} ({RoutingKey}) was not sent.",
            eventId, routingKey);
        return Task.CompletedTask;
    }
}
