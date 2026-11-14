using SwiftDrop.Merchants.Contracts;

namespace SwiftDrop.Merchants.Business.Services;

public interface IMerchantEventPublisher
{
    Task PublishAsync(MerchantRegisteredEvent message, CancellationToken ct = default);
    Task PublishAsync(MerchantApprovedEvent message, CancellationToken ct = default);
}

internal sealed class UnconfiguredMerchantEventPublisher : IMerchantEventPublisher
{
    public Task PublishAsync(MerchantRegisteredEvent message, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task PublishAsync(MerchantApprovedEvent message, CancellationToken ct = default) =>
        Task.CompletedTask;
}

