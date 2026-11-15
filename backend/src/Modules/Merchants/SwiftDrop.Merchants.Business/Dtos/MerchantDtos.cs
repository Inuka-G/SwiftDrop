using System.Text.Json.Serialization;

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

public sealed record CreateClientCredentialCommand(string Name);

public sealed record CreatedClientCredentialDto(
    Guid Id,
    string Name,
    string ClientId,
    string PlainTextClientSecret,
    DateTime CreatedAtUtc);

public sealed record ClientCredentialSummaryDto(
    Guid Id,
    string Name,
    string ClientId,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? RevokedAtUtc);

public sealed record OAuthTokenRequest
{
    [JsonPropertyName("grant_type")]
    public string? GrantType { get; init; }

    [JsonPropertyName("client_id")]
    public string? ClientId { get; init; }

    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; init; }

    [JsonPropertyName("scope")]
    public string? Scope { get; init; }
}

public sealed record OAuthTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("scope")] string? Scope = null);

