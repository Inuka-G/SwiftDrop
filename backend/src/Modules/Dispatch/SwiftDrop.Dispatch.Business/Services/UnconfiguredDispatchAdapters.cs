namespace SwiftDrop.Dispatch.Business.Services;

// Host-safe placeholders until Identity, Parcels, and SharedKernel expose their adapters.
internal sealed class UnconfiguredDispatchAdapters : ICourierEligibilityGateway,
    IParcelAssignmentGateway, IDispatchEventPublisher
{
    private static InvalidOperationException Missing(string contract) =>
        new($"Dispatch adapter is not configured: {contract}.");

    public Task<bool> IsEligibleAsync(Guid courierId, CancellationToken ct) =>
        throw Missing("Identity courier eligibility");

    public Task<bool> AssignAsync(Guid parcelId, Guid courierId, CancellationToken ct) =>
        throw Missing("Parcels assignment gateway");

    public Task PublishAsync(SwiftDrop.Dispatch.Contracts.CourierAssignedEvent message, CancellationToken ct) =>
        throw Missing("SharedKernel/RabbitMQ event publisher");
}
