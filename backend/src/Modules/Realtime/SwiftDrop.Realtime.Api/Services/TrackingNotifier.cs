
using Microsoft.AspNetCore.SignalR;
using SwiftDrop.Realtime.Api.Hubs;
using SwiftDrop.Realtime.Business.DTOs;
using SwiftDrop.Realtime.Business.Services;

namespace SwiftDrop.Realtime.Api.Services;

public sealed class TrackingNotifier(
    IHubContext<TrackingHub> hubContext
) : ITrackingNotifier
{
    public async Task NotifyStatusChangedAsync(
        DeliveryStatusUpdate update,
        CancellationToken cancellationToken = default)
    {
        var groupName = GetTrackingGroupName(update.TrackingNumber);

        await hubContext.Clients
            .Group(groupName)
            .SendAsync(
                "DeliveryStatusUpdated",
                update,
                cancellationToken
            );
    }

    private static string GetTrackingGroupName(string trackingNumber)
    {
        return $"tracking-{trackingNumber.Trim().ToUpperInvariant()}";
    }
}
