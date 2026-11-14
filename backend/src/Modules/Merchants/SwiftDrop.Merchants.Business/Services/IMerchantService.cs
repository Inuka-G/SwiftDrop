using SwiftDrop.Merchants.Business.Dtos;

namespace SwiftDrop.Merchants.Business.Services;

public interface IMerchantService
{
    Task<MerchantDto> RegisterAsync(RegisterMerchantCommand command, Guid? userId, CancellationToken ct = default);
    Task<IReadOnlyList<MerchantApprovalQueueItemDto>> GetPendingQueueAsync(CancellationToken ct = default);
    Task<MerchantDto> ApproveAsync(Guid merchantId, Guid adminId, CancellationToken ct = default);
    Task<MerchantDto> RejectAsync(Guid merchantId, Guid adminId, CancellationToken ct = default);
    Task<MerchantDto> GetByIdAsync(Guid merchantId, CancellationToken ct = default);
    Task<MerchantDto?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<CreatedApiKeyDto> CreateApiKeyAsync(Guid merchantId, CreateApiKeyCommand command, CancellationToken ct = default);
    Task<IReadOnlyList<ApiKeySummaryDto>> GetApiKeysAsync(Guid merchantId, CancellationToken ct = default);
    Task RevokeApiKeyAsync(Guid merchantId, Guid keyId, CancellationToken ct = default);
    Task<CreatedClientCredentialDto> CreateClientCredentialAsync(Guid merchantId, CreateClientCredentialCommand command, CancellationToken ct = default);
    Task<IReadOnlyList<ClientCredentialSummaryDto>> GetClientCredentialsAsync(Guid merchantId, CancellationToken ct = default);
    Task RevokeClientCredentialAsync(Guid merchantId, Guid credentialId, CancellationToken ct = default);
    Task<OAuthTokenResponse> ExchangeClientCredentialsAsync(OAuthTokenRequest request, CancellationToken ct = default);
}

