using SwiftDrop.Dispatch.Contracts;

namespace SwiftDrop.Dispatch.Business.Services;

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
    Task<SwiftDrop.Parcels.Contracts.CourierAssignmentResult?> AssignAsync(Guid parcelId, Guid courierId, CancellationToken ct);
}

internal interface IDispatchEventPublisher
{
    Task PublishAsync(CourierAssignedEvent message, CancellationToken ct);
}
