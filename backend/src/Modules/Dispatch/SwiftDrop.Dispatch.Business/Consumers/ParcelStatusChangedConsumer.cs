using SwiftDrop.Dispatch.Contracts;
using SwiftDrop.Dispatch.Data.Repositories;

namespace SwiftDrop.Dispatch.Business.Consumers;

internal sealed class ParcelStatusChangedConsumer(IDispatchRepository repository)
{
    public Task HandleAsync(ParcelStatusChangedMessage message, CancellationToken ct) =>
        repository.ApplyParcelStatusChangedAsync(message.EventId, message.ParcelId, message.Status, ct);
}
