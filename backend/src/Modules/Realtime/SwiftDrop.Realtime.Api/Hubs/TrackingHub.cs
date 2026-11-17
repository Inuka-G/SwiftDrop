
using Microsoft.AspNetCore.SignalR;

namespace SwiftDrop.Realtime.Api.Hubs;

public sealed class TrackingHub : Hub
{
    public async Task SubscribeToTracking(string trackingNumber)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
        {
            throw new HubException("Tracking number is required.");
        }

        var groupName = GetTrackingGroupName(trackingNumber);

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task UnsubscribeFromTracking(string trackingNumber)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
        {
            throw new HubException("Tracking number is required.");
        }

        var groupName = GetTrackingGroupName(trackingNumber);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    private static string GetTrackingGroupName(string trackingNumber)
    {
        return $"tracking-{trackingNumber.Trim().ToUpperInvariant()}";
    }
}
