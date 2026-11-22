using System.Reflection;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace SwiftDrop.SharedKernel.Messaging;

internal sealed class RabbitMqEventPublisher(RabbitMqConnection connection) : IEventPublisher, IIntegrationEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IntegrationEvent
    {
        var eventType = @event.GetType();
        var routingKey = eventType.GetCustomAttribute<RoutingKeyAttribute>()?.Key
            ?? typeof(TEvent).GetCustomAttribute<RoutingKeyAttribute>()?.Key
            ?? throw new InvalidOperationException($"{eventType.Name} has no [RoutingKey].");

        var body = JsonSerializer.SerializeToUtf8Bytes(@event, eventType, JsonSerializerOptions.Web);
        return PublishAsync(@event.EventId, routingKey, body, eventType.Name, mandatory: false, ct);
    }

    public Task PublishAsync(Guid eventId, string routingKey, string payloadJson, CancellationToken ct) =>
        PublishAsync(eventId, routingKey, Encoding.UTF8.GetBytes(payloadJson), eventType: null,
            connection.Options.RequiredOutboxRoutes.Contains(routingKey, StringComparer.Ordinal), ct);

    private async Task PublishAsync(Guid eventId, string routingKey, ReadOnlyMemory<byte> body,
        string? eventType, bool mandatory, CancellationToken ct)
    {
        var conn = await connection.GetAsync(ct);
        // publisher confirms: PublishAsync only completes once the broker has accepted the message
        await using var channel = await conn.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true,
                                     publisherConfirmationTrackingEnabled: true),
            ct);

        await connection.DeclareExchangesAsync(channel, ct);

        var props = new BasicProperties
        {
            Persistent = true,                       // survive broker restart
            ContentType = "application/json",
            MessageId = eventId.ToString(),
            Type = eventType
        };

        await channel.BasicPublishAsync(connection.Options.Exchange, routingKey,
            mandatory: mandatory, basicProperties: props, body: body, cancellationToken: ct);
    }
}
