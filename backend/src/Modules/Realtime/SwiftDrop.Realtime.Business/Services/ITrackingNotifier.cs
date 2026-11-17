
using SwiftDrop.Realtime.Business.DTOs;

namespace SwiftDrop.Realtime.Business.Services;

public interface ITrackingNotifier
{
    Task NotifyStatusChangedAsync(
        DeliveryStatusUpdate update,
        CancellationToken cancellationToken = default
    );
}
