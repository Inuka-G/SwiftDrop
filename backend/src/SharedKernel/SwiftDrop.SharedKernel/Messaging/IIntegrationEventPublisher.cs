namespace SwiftDrop.SharedKernel.Messaging;

/// Sends an integration event to the message broker (RabbitMQ topic exchange "swiftdrop.events").
/// Modules never call this directly from a request: they write an outbox row in the same
/// transaction as their change, and a background publisher calls this afterwards.
/// The routing key names the event, for example "parcels.status-changed".
public interface IIntegrationEventPublisher
{
    Task PublishAsync(Guid eventId, string routingKey, string payloadJson, CancellationToken ct);
}
