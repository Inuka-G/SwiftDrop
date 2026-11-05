using SwiftDrop.Dispatch.Contracts;

namespace SwiftDrop.Dispatch.Business.Services;

// Adapters are intentionally not implemented until the owning module contracts exist.
public interface ICourierEligibilityGateway
{
    Task<bool> IsEligibleAsync(Guid courierId, CancellationToken ct);
}

public interface IParcelAssignmentGateway
{
    Task<bool> AssignAsync(Guid parcelId, Guid courierId, CancellationToken ct);
}

public interface IDispatchEventPublisher
{
    Task PublishAsync(CourierAssignedEvent message, CancellationToken ct);
}
