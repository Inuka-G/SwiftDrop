using Microsoft.Extensions.Logging;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.Notifications.Business.Handlers;

internal sealed class ParcelStatusNotificationHandler(ILogger<ParcelStatusNotificationHandler> log)
    : IIntegrationEventHandler<ParcelStatusChangedIntegrationEvent>
{
    public Task HandleAsync(ParcelStatusChangedIntegrationEvent e, CancellationToken ct)
    {
        // later: IUserDirectory.GetContactAsync(e.CustomerId) then IEmailSender.SendAsync(...)
        log.LogInformation("Notify: parcel {Tracking} moved {Old} -> {New}",
            e.TrackingNumber, e.PreviousStatus, e.NewStatus);
        return Task.CompletedTask;
    }
}

