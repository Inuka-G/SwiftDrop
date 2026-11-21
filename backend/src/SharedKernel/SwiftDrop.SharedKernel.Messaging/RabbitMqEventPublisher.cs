using System.Reflection;
using System.Text.Json;
using RabbitMQ.Client;

namespace SwiftDrop.SharedKernel.Messaging;

internal sealed class RabbitMqEventPublisher(RabbitMqConnection connection) : IEventPublisher
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IntegrationEvent
    {
        var eventType = @event.GetType();
        var routingKey = eventType.GetCustomAttribute<RoutingKeyAttribute>()?.Key
            ?? typeof(TEvent).GetCustomAttribute<RoutingKeyAttribute>()?.Key
            ?? throw new InvalidOperationException($"{eventType.Name} has no [RoutingKey].");

        var conn = await connection.GetAsync(ct);
        // publisher confirms: PublishAsync only completes once the broker has accepted the message
        await using var channel = await conn.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true,
                                     publisherConfirmationTrackingEnabled: true),
            ct);

        await connection.DeclareExchangesAsync(channel, ct);

        var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), JsonSerializerOptions.Web);
        var props = new BasicProperties
        {
            Persistent = true,                       // survive broker restart
            ContentType = "application/json",
            MessageId = @event.EventId.ToString(),
            Type = eventType.Name
        };

        await channel.BasicPublishAsync(connection.Options.Exchange, routingKey,
            mandatory: false, basicProperties: props, body: body, cancellationToken: ct);
    }
}