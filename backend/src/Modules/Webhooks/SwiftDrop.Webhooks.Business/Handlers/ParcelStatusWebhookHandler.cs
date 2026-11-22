using Microsoft.Extensions.Logging;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.Webhooks.Business.Handlers;

internal sealed class ParcelStatusWebhookHandler(ILogger<ParcelStatusWebhookHandler> log)
    : IIntegrationEventHandler<ParcelStatusChanged>
{
    public Task HandleAsync(ParcelStatusChanged e, CancellationToken ct)
    {
        log.LogInformation("Webhook: parcel {Tracking} moved {Old} -> {New} (MerchantId: {MerchantId})",
            e.TrackingNumber, e.OldStatus, e.NewStatus, e.MerchantId);
        return Task.CompletedTask;
    }
}
