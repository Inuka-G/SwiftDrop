using System.Text.Json;
using SwiftDrop.Parcels.Data.Entities;
using SwiftDrop.Parcels.Data.Repositories;

namespace SwiftDrop.Parcels.Business.Lifecycle;

internal static class OutboxStaging
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// Adds the event to the outbox. It is written by the caller's next SaveChangesAsync,
    /// in the same transaction as the parcel change it describes.
    public static void StageEvent<TEvent>(this IParcelRepository repository, Guid eventId, string routingKey,
        TEvent integrationEvent, DateTime occurredAtUtc) =>
        repository.AddOutboxMessage(new OutboxMessage(eventId, routingKey,
            JsonSerializer.Serialize(integrationEvent, Json), occurredAtUtc));
}
