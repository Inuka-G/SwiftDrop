using SwiftDrop.Dispatch.Contracts;
using SwiftDrop.Dispatch.Data.Entities;
using SwiftDrop.Dispatch.Data.Repositories;

namespace SwiftDrop.Dispatch.Business.Consumers;

internal sealed class ParcelCreatedConsumer(IDispatchRepository repository)
{
    public Task HandleAsync(ParcelCreatedMessage message, CancellationToken ct) =>
        repository.AddFromEventAsync(message.EventId, new ParcelAssignment
        {
            ParcelId = message.ParcelId,
            TrackingNumber = message.TrackingNumber,
            ZoneId = message.ZoneId,
            CreatedAt = message.CreatedAt,
            State = AssignmentStates.Unassigned,
            Version = 0
        }, ct);
}
