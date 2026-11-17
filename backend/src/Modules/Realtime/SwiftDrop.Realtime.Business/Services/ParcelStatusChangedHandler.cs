
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.Realtime.Business.DTOs;

namespace SwiftDrop.Realtime.Business.Services;

public sealed class ParcelStatusChangedHandler(
    ITrackingNotifier trackingNotifier
)
{
    public async Task HandleAsync(
        ParcelStatusChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var update = new DeliveryStatusUpdate(
            integrationEvent.ParcelId,
            integrationEvent.TrackingNumber,
            integrationEvent.NewStatus,
            integrationEvent.OccurredAtUtc
        );

        await trackingNotifier.NotifyStatusChangedAsync(
            update,
            cancellationToken
        );
    }
}
