namespace SwiftDrop.Merchants.Business.Dtos;

public sealed record RegisterMerchantCommand(
    string BusinessName,
    string BusinessRegNumber,
    string? WebhookUrl);

public sealed record MerchantDto(
    Guid Id,
    string BusinessName,
    string BusinessRegNumber,
    string Status,
    string? WebhookUrl,
    Guid? UserId,
    Guid? ApprovedBy,
    DateTimeOffset? ApprovedAt,
    DateTime CreatedAtUtc);

public sealed record MerchantApprovalQueueItemDto(
    Guid Id,
    string BusinessName,
    string BusinessRegNumber,
    string Status,
    Guid? UserId,
    DateTime CreatedAtUtc);

public sealed record CreateApiKeyCommand(string Name);

public sealed record CreatedApiKeyDto(
    Guid Id,
    string Name,
    string KeyPrefix,
    string PlainTextKey,
    DateTime CreatedAtUtc);

public sealed record ApiKeySummaryDto(
    Guid Id,
    string Name,
    string KeyPrefix,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? RevokedAtUtc);

