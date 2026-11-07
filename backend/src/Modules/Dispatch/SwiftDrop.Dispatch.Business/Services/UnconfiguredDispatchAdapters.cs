namespace SwiftDrop.Dispatch.Business.Services;

// Host-safe placeholders until Identity, Parcels, and SharedKernel expose their adapters.
internal sealed class UnconfiguredDispatchAdapters : ICourierEligibilityGateway,
    IParcelAssignmentGateway, IDispatchEventPublisher, IZoneLookup
{
    private static InvalidOperationException Missing(string contract) =>
        new($"Dispatch adapter is not configured: {contract}.");

    public Task<bool> IsEligibleAsync(Guid courierId, CancellationToken ct) =>
        throw Missing("Identity courier eligibility");

    public Task<bool> IsActiveCourierAsync(Guid userId, CancellationToken ct) =>
        throw Missing("Identity active courier lookup");

    public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct) =>
        throw Missing("Identity courier name lookup");

    public Task<bool> ExistsAsync(Guid zoneId, CancellationToken ct) =>
        throw Missing("Pricing zone lookup");

    public Task<bool> AssignAsync(Guid parcelId, Guid courierId, CancellationToken ct) =>
        throw Missing("Parcels assignment gateway");

    public Task PublishAsync(SwiftDrop.Dispatch.Contracts.CourierAssignedEvent message, CancellationToken ct) =>
        throw Missing("SharedKernel/RabbitMQ event publisher");
}
