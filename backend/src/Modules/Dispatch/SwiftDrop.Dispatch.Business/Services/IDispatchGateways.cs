using SwiftDrop.Dispatch.Contracts;

namespace SwiftDrop.Dispatch.Business.Services;

// Adapters are intentionally not implemented until the owning module contracts exist.
internal interface ICourierEligibilityGateway
{
    Task<bool> IsEligibleAsync(Guid courierId, CancellationToken ct);
    Task<bool> IsActiveCourierAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct);
}

internal interface IZoneLookup
{
    Task<bool> ExistsAsync(Guid zoneId, CancellationToken ct);
}

internal interface IParcelAssignmentGateway
{
    Task<bool> AssignAsync(Guid parcelId, Guid courierId, CancellationToken ct);
}

internal interface IDispatchEventPublisher
{
    Task PublishAsync(CourierAssignedEvent message, CancellationToken ct);
}
