using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.Realtime.Business.DTOs;
using SwiftDrop.Realtime.Business.Services;
using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.Realtime.Business.Services;

public sealed class ParcelStatusChangedHandler(
    ITrackingNotifier trackingNotifier
) : IIntegrationEventHandler<ParcelStatusChanged>
{
    public async Task HandleAsync(
        ParcelStatusChanged integrationEvent,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var update = new DeliveryStatusUpdate(
            integrationEvent.ParcelId,
            integrationEvent.TrackingNumber,
            integrationEvent.NewStatus,
            integrationEvent.OccurredOnUtc
        );

        await trackingNotifier.NotifyStatusChangedAsync(update, ct);
    }
}