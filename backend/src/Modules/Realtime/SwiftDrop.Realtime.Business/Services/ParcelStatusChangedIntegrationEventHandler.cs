using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.Realtime.Business.DTOs;
using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.Realtime.Business.Services;

public sealed class ParcelStatusChangedIntegrationEventHandler(
    ITrackingNotifier trackingNotifier
) : IIntegrationEventHandler<ParcelStatusChangedIntegrationEvent>
{
    public async Task HandleAsync(
        ParcelStatusChangedIntegrationEvent integrationEvent,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var update = new DeliveryStatusUpdate(
            integrationEvent.ParcelId,
            integrationEvent.TrackingNumber,
            integrationEvent.NewStatus,
            integrationEvent.OccurredAtUtc);

        await trackingNotifier.NotifyStatusChangedAsync(update, ct);
    }
}
