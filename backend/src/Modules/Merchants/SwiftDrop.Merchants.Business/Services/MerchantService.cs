using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using SwiftDrop.Merchants.Business.Dtos;
using SwiftDrop.Merchants.Contracts;
using SwiftDrop.Merchants.Data.Entities;
using SwiftDrop.Merchants.Data.Repositories;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Merchants.Business.Services;

internal sealed class MerchantService(
    IMerchantRepository repository,
    IValidator<RegisterMerchantCommand> validator,
    IValidator<CreateApiKeyCommand> apiKeyValidator,
    IValidator<CreateClientCredentialCommand> clientCredentialValidator,
    IOAuthTokenService oauthTokenService,
    IMerchantEventPublisher eventPublisher) : IMerchantService, IMerchantAccess
{
    public async Task<MerchantDto> RegisterAsync(RegisterMerchantCommand command, Guid? userId, CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            throw new BadRequestException(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var existing = await repository.GetByRegNumberAsync(command.BusinessRegNumber, ct);
        if (existing is not null)
            throw new ConflictException("A merchant with this business registration number already exists.");

        if (userId.HasValue)
        {
            var existingUserMerchant = await repository.GetByUserIdAsync(userId.Value, ct);
            if (existingUserMerchant is not null)
                throw new ConflictException("You have already registered a merchant account.");
        }

        var merchant = new Merchant(command.BusinessName, command.BusinessRegNumber, userId, command.WebhookUrl);
        await repository.AddAsync(merchant, ct);
        await repository.SaveChangesAsync(ct);

        await eventPublisher.PublishAsync(
            new MerchantRegisteredEvent(Guid.NewGuid(), merchant.Id, merchant.BusinessName, DateTimeOffset.UtcNow), ct);

        return ToDto(merchant);
    }

    public async Task<IReadOnlyList<MerchantApprovalQueueItemDto>> GetPendingQueueAsync(CancellationToken ct = default)
    {
        var pending = await repository.GetPendingAsync(ct);
        return pending.Select(m => new MerchantApprovalQueueItemDto(
            m.Id, m.BusinessName, m.BusinessRegNumber, m.Status, m.UserId, m.CreatedAtUtc)).ToList();
    }

    public async Task<MerchantDto> ApproveAsync(Guid merchantId, Guid adminId, CancellationToken ct = default)
    {
        var merchant = await repository.GetByIdAsync(merchantId, ct)
            ?? throw new NotFoundException("Merchant not found.");

        if (merchant.Status != MerchantStatus.Pending)
            throw new ConflictException($"Cannot approve merchant in '{merchant.Status}' status. Only pending merchants can be approved.");

        merchant.Approve(adminId);
        await repository.SaveChangesAsync(ct);

        await eventPublisher.PublishAsync(
            new MerchantApprovedEvent(Guid.NewGuid(), merchant.Id, merchant.BusinessName, DateTimeOffset.UtcNow), ct);

        return ToDto(merchant);
    }

    public async Task<MerchantDto> RejectAsync(Guid merchantId, Guid adminId, CancellationToken ct = default)
    {
        var merchant = await repository.GetByIdAsync(merchantId, ct)
            ?? throw new NotFoundException("Merchant not found.");

        if (merchant.Status != MerchantStatus.Pending)
            throw new ConflictException($"Cannot reject merchant in '{merchant.Status}' status. Only pending merchants can be rejected.");

        merchant.Reject(adminId);
        await repository.SaveChangesAsync(ct);

        return ToDto(merchant);
    }

    public async Task<MerchantDto> GetByIdAsync(Guid merchantId, CancellationToken ct = default)
    {
        var merchant = await repository.GetByIdAsync(merchantId, ct)
            ?? throw new NotFoundException("Merchant not found.");

        return ToDto(merchant);
    }

    public async Task<MerchantDto?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var merchant = await repository.GetByUserIdAsync(userId, ct);
        return merchant is null ? null : ToDto(merchant);
    }

    public async Task<CreatedApiKeyDto> CreateApiKeyAsync(Guid merchantId, CreateApiKeyCommand command, CancellationToken ct = default)
    {
        var validation = await apiKeyValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            throw new BadRequestException(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var merchant = await repository.GetByIdAsync(merchantId, ct)
            ?? throw new NotFoundException("Merchant not found.");

        if (merchant.Status != MerchantStatus.Approved)
            throw new ForbiddenException("Only approved merchants can create API keys.");

        var randomBytes = RandomNumberGenerator.GetBytes(16);
        var rawKey = $"sd_live_{Convert.ToHexString(randomBytes).ToLowerInvariant()}";
        var keyPrefix = rawKey[..16];
        var keyHash = HashKey(rawKey);

        var apiKey = new ApiKey(merchant.Id, command.Name, keyPrefix, keyHash);
        await repository.AddApiKeyAsync(apiKey, ct);
        await repository.SaveChangesAsync(ct);

        return new CreatedApiKeyDto(apiKey.Id, apiKey.Name, apiKey.KeyPrefix, rawKey, apiKey.CreatedAtUtc);
    }

    public async Task<IReadOnlyList<ApiKeySummaryDto>> GetApiKeysAsync(Guid merchantId, CancellationToken ct = default)
    {
        var merchant = await repository.GetByIdAsync(merchantId, ct)
            ?? throw new NotFoundException("Merchant not found.");

        var keys = await repository.GetApiKeysByMerchantIdAsync(merchant.Id, ct);
        return keys.Select(k => new ApiKeySummaryDto(
            k.Id, k.Name, k.KeyPrefix, k.IsActive, k.CreatedAtUtc, k.RevokedAtUtc)).ToList();
    }

    public async Task RevokeApiKeyAsync(Guid merchantId, Guid keyId, CancellationToken ct = default)
    {
        var key = await repository.GetApiKeyByIdAsync(keyId, ct)
            ?? throw new NotFoundException("API key not found.");

        if (key.MerchantId != merchantId)
            throw new ForbiddenException("You cannot revoke an API key that belongs to another merchant.");

        key.Revoke();
        await repository.SaveChangesAsync(ct);
    }

    public async Task<CreatedClientCredentialDto> CreateClientCredentialAsync(
        Guid merchantId, CreateClientCredentialCommand command, CancellationToken ct = default)
    {
        var validation = await clientCredentialValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            throw new BadRequestException(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var merchant = await repository.GetByIdAsync(merchantId, ct)
            ?? throw new NotFoundException("Merchant not found.");

        if (merchant.Status != MerchantStatus.Approved)
            throw new ForbiddenException("Only approved merchants can create OAuth client credentials.");

        var clientId = $"sd_client_{Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant()}";
        var rawSecret = $"sd_secret_{Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant()}";
        var secretHash = HashKey(rawSecret);

        var credential = new ClientCredential(merchant.Id, command.Name, clientId, secretHash);
        await repository.AddClientCredentialAsync(credential, ct);
        await repository.SaveChangesAsync(ct);

        return new CreatedClientCredentialDto(
            credential.Id, credential.Name, credential.ClientId, rawSecret, credential.CreatedAtUtc);
    }

    public async Task<IReadOnlyList<ClientCredentialSummaryDto>> GetClientCredentialsAsync(
        Guid merchantId, CancellationToken ct = default)
    {
        var merchant = await repository.GetByIdAsync(merchantId, ct)
            ?? throw new NotFoundException("Merchant not found.");

        var creds = await repository.GetClientCredentialsByMerchantIdAsync(merchant.Id, ct);
        return creds.Select(c => new ClientCredentialSummaryDto(
            c.Id, c.Name, c.ClientId, c.IsActive, c.CreatedAtUtc, c.RevokedAtUtc)).ToList();
    }

    public async Task RevokeClientCredentialAsync(
        Guid merchantId, Guid credentialId, CancellationToken ct = default)
    {
        var cred = await repository.GetClientCredentialByIdAsync(credentialId, ct)
            ?? throw new NotFoundException("Client credential not found.");

        if (cred.MerchantId != merchantId)
            throw new ForbiddenException("You cannot revoke client credentials that belong to another merchant.");

        cred.Revoke();
        await repository.SaveChangesAsync(ct);
    }

    public async Task<OAuthTokenResponse> ExchangeClientCredentialsAsync(
        OAuthTokenRequest request, CancellationToken ct = default)
    {
        if (!string.Equals(request.GrantType, "client_credentials", StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException("Unsupported grant type. Only 'client_credentials' is supported.");

        if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.ClientSecret))
            throw new BadRequestException("client_id and client_secret are required.");

        var cred = await repository.GetClientCredentialByClientIdAsync(request.ClientId.Trim(), ct);
        if (cred is null || !cred.IsActive || cred.ClientSecretHash != HashKey(request.ClientSecret.Trim()))
            throw new UnauthorizedException("Invalid client credentials.");

        var merchant = await repository.GetByIdAsync(cred.MerchantId, ct);
        if (merchant is null || merchant.Status != MerchantStatus.Approved)
            throw new ForbiddenException("Merchant account is not active or approved.");

        return oauthTokenService.IssueMerchantToken(merchant.Id, merchant.UserId, merchant.BusinessName, cred.ClientId);
    }

    // --- IMerchantAccess Implementation (Cross-Module Contract) ---

    public async Task<MerchantAccessResult?> ValidateApiKeyAsync(string rawApiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawApiKey))
            return null;

        var hash = HashKey(rawApiKey.Trim());
        var key = await repository.GetApiKeyByHashAsync(hash, ct);
        if (key is null || !key.IsActive)
            return null;

        var merchant = await repository.GetByIdAsync(key.MerchantId, ct);
        if (merchant is null || merchant.Status != MerchantStatus.Approved)
            return null;

        return new MerchantAccessResult(merchant.Id, merchant.BusinessName, IsActive: true);
    }

    private static string HashKey(string key)
    {
        var bytes = Encoding.UTF8.GetBytes(key);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public async Task<MerchantWebhookTarget?> GetWebhookTargetAsync(Guid merchantId, CancellationToken ct = default)
    {
        var merchant = await repository.GetByIdAsync(merchantId, ct);
        if (merchant is null || merchant.Status != MerchantStatus.Approved || string.IsNullOrWhiteSpace(merchant.WebhookUrl))
            return null;

        return new MerchantWebhookTarget(merchant.Id, merchant.WebhookUrl, merchant.WebhookSecret);
    }

    private static MerchantDto ToDto(Merchant m) =>
        new(m.Id, m.BusinessName, m.BusinessRegNumber, m.Status, m.WebhookUrl, m.UserId, m.ApprovedBy, m.ApprovedAt, m.CreatedAtUtc);
}

