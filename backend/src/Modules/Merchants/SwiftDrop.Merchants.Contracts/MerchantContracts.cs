namespace SwiftDrop.Merchants.Contracts;

/// <summary>
/// The result returned when another module (like Parcels) asks to validate an API key.
/// </summary>
public sealed record MerchantAccessResult(
    Guid MerchantId,
    string BusinessName,
    bool IsActive);

/// <summary>
/// Webhook connection details for a merchant, used by the Webhooks module to send status updates.
/// </summary>
public sealed record MerchantWebhookTarget(
    Guid MerchantId,
    string WebhookUrl,
    string? WebhookSecret);

/// <summary>
/// Public interface exposed to other modules in the monolith.
/// </summary>
public interface IMerchantAccess
{
    Task<MerchantAccessResult?> ValidateApiKeyAsync(string rawApiKey, CancellationToken ct = default);
    Task<MerchantWebhookTarget?> GetWebhookTargetAsync(Guid merchantId, CancellationToken ct = default);
}

/// <summary>
/// Events published to notify other modules (like Notifications) when merchant status changes.
/// </summary>
public sealed record MerchantRegisteredEvent(
    Guid EventId,
    Guid MerchantId,
    string BusinessName,
    DateTimeOffset OccurredAt);

public sealed record MerchantApprovedEvent(
    Guid EventId,
    Guid MerchantId,
    string BusinessName,
    DateTimeOffset OccurredAt);

