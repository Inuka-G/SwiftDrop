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

    // --- IMerchantAccess Implementation (Cross-Module Contract) ---

    public Task<MerchantAccessResult?> ValidateApiKeyAsync(string rawApiKey, CancellationToken ct = default)
    {
        // Will be implemented in Concept 2 (API Keys)
        return Task.FromResult<MerchantAccessResult?>(null);
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

