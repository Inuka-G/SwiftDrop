using System.Reflection;
using System.Text.Json;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.Parcels.Data.Entities;
using SwiftDrop.Parcels.Data.Repositories;
using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.Parcels.Business.Lifecycle;

internal static class OutboxStaging
{
    /// Adds the event to the outbox. It is written by the caller's next SaveChangesAsync,
    /// in the same transaction as the parcel change it describes, and published later
    /// by the OutboxProcessor. Nothing in Parcels publishes to the broker directly.
    public static void StageEvent(this IParcelRepository repository, IntegrationEvent integrationEvent)
    {
        var type = integrationEvent.GetType();
        repository.AddOutboxMessage(new OutboxMessage(integrationEvent.EventId, ParcelEventTypes.RoutingKeyOf(type),
            JsonSerializer.Serialize(integrationEvent, type, JsonSerializerOptions.Web), integrationEvent.OccurredOnUtc));
    }
}

/// Maps routing keys back to the Parcels event types, so the outbox can rebuild the
/// typed event before handing it to the RabbitMQ publisher.
internal static class ParcelEventTypes
{
    private static readonly Dictionary<string, Type> ByRoutingKey = typeof(ParcelCreated).Assembly.GetTypes()
        .Where(t => t.IsSubclassOf(typeof(IntegrationEvent)) && !t.IsAbstract)
        .ToDictionary(RoutingKeyOf);

    public static string RoutingKeyOf(Type eventType) =>
        eventType.GetCustomAttribute<RoutingKeyAttribute>()?.Key
        ?? throw new InvalidOperationException($"{eventType.Name} has no [RoutingKey].");

    public static Type? Find(string routingKey) => ByRoutingKey.GetValueOrDefault(routingKey);
}
