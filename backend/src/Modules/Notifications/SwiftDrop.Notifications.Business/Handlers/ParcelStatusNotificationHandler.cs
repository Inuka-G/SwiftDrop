using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftDrop.Identity.Contracts;
using SwiftDrop.Merchants.Contracts;
using SwiftDrop.Notifications.Business.Email;
using SwiftDrop.Notifications.Data;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.Notifications.Business.Handlers;

internal sealed class ParcelStatusNotificationHandler(
    IOptions<NotificationEmailOptions> options,
    IUserEmailDirectory users,
    IMerchantContactDirectory merchants,
    NotificationDeliveryStore deliveries,
    INotificationEmailSender sender,
    ILogger<ParcelStatusNotificationHandler> log)
    : IIntegrationEventHandler<ParcelStatusChangedIntegrationEvent>
{
    public async Task HandleAsync(ParcelStatusChangedIntegrationEvent evt, CancellationToken ct)
    {
        if (!options.Value.Enabled)
            return;

        if (evt.EventId == Guid.Empty || string.IsNullOrWhiteSpace(evt.TrackingNumber))
            throw new InvalidOperationException("The status event is missing its identity or tracking number.");

        var userId = evt.CustomerId;
        if (userId is null && evt.MerchantId is { } merchantId)
            userId = await merchants.GetApprovedContactUserIdAsync(merchantId, ct);

        var email = userId is { } id ? await users.GetActiveEmailAsync(id, ct) : null;
        if (!MailAddress.TryCreate(email, out var recipient))
        {
            log.LogInformation("No email recipient available for event {EventId}.", evt.EventId);
            return;
        }

        var baseUrl = options.Value.TrackingBaseUrl.TrimEnd('/');
        var link = $"{baseUrl}/track/{Uri.EscapeDataString(evt.TrackingNumber)}";
        var body = $"Tracking number: {evt.TrackingNumber}\nCurrent status: {evt.NewStatus}\nTrack your parcel: {link}";
        var senderHost = new MailAddress(options.Value.FromAddress).Host;
        var messageId = $"<swiftdrop-{evt.EventId:N}@{senderHost}>";
        await deliveries.SendOnceAsync(evt.EventId,
            token => sender.SendAsync(recipient, "SwiftDrop parcel status update", body, messageId, token), ct);
    }
}
