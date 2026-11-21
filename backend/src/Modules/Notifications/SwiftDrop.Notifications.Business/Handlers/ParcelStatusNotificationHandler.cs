using Microsoft.Extensions.Logging;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.Notifications.Business.Handlers;

internal sealed class ParcelStatusNotificationHandler(ILogger<ParcelStatusNotificationHandler> log)
    : IIntegrationEventHandler<ParcelStatusChanged>
{
    public Task HandleAsync(ParcelStatusChanged e, CancellationToken ct)
    {
        // later: IUserDirectory.GetContactAsync(e.CustomerId) then IEmailSender.SendAsync(...)
        log.LogInformation("Notify: parcel {Tracking} moved {Old} -> {New}",
            e.TrackingNumber, e.OldStatus, e.NewStatus);
        return Task.CompletedTask;
    }
}

