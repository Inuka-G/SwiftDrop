using SwiftDrop.Dispatch.Data.Entities;

namespace SwiftDrop.Dispatch.Data.Repositories;

internal interface IDispatchRepository
{
    Task<(IReadOnlyList<ParcelAssignment> Items, int Total)> ListUnassignedAsync(Guid? zoneId,
        DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken ct);
    Task<ParcelAssignment?> GetAsync(Guid parcelId, CancellationToken ct);
    Task<CourierProfile?> GetCourierProfileAsync(Guid courierId, CancellationToken ct);
    Task AddCourierProfileAsync(CourierProfile profile, CancellationToken ct);
    Task<(IReadOnlyList<CourierProfile> Items, int Total)> ListCouriersAsync(Guid? zoneId, bool? isAvailable,
        int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyDictionary<Guid, int>> GetWorkloadsAsync(IReadOnlyCollection<Guid> courierIds, CancellationToken ct);
    Task<(int Total, int Available, int Unavailable, int ActiveDeliveries)> GetCourierSummaryAsync(CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
    Task AddFromEventAsync(Guid eventId, ParcelAssignment assignment, CancellationToken ct);
    Task ApplyParcelStatusChangedAsync(Guid eventId, Guid parcelId, string status, CancellationToken ct);
    Task ApplyUserRoleChangedAsync(Guid eventId, Guid userId, string role, CancellationToken ct);
    Task ApplyUserStatusChangedAsync(Guid eventId, Guid userId, bool isActive, CancellationToken ct);
}
