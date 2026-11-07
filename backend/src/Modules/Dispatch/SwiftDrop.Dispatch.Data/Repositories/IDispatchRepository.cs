using SwiftDrop.Dispatch.Data.Entities;

namespace SwiftDrop.Dispatch.Data.Repositories;

internal interface IDispatchRepository
{
    Task<(IReadOnlyList<ParcelAssignment> Items, int Total)> ListUnassignedAsync(Guid? zoneId,
        DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken ct);
    Task<ParcelAssignment?> GetAsync(Guid parcelId, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
    Task AddFromEventAsync(Guid eventId, ParcelAssignment assignment, CancellationToken ct);
}
