using SwiftDrop.Dispatch.Data.Entities;
using SwiftDrop.Dispatch.Contracts;
using SwiftDrop.Parcels.Contracts;

namespace SwiftDrop.Dispatch.Data.Repositories;

public sealed record DispatchOutboxRecord(Guid EventId, string RoutingKey, string Payload);

internal interface IDispatchRepository
{
    Task<(IReadOnlyList<ParcelAssignment> Items, int Total)> ListUnassignedAsync(Guid? zoneId,
        DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken ct);
    Task ReconcileParcelsAsync(IReadOnlyCollection<DispatchableParcelSnapshot> parcels, CancellationToken ct);
    Task<(IReadOnlyList<ParcelAssignment> Items, int Total)> ListParcelsAsync(string? status,
        string? trackingNumber, int page, int pageSize, string? assignmentState, CancellationToken ct);
    Task<ParcelAssignment?> GetAsync(Guid parcelId, CancellationToken ct);
    Task<CourierProfile?> GetCourierProfileAsync(Guid courierId, CancellationToken ct);
    Task AddCourierProfileAsync(CourierProfile profile, CancellationToken ct);
    Task<CourierProfileDeleteStatus> DeleteCourierProfileAsync(Guid courierId, CancellationToken ct);
    Task<(IReadOnlyList<CourierProfile> Items, int Total)> ListCouriersAsync(Guid? zoneId, bool? isAvailable,
        int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyDictionary<Guid, int>> GetWorkloadsAsync(IReadOnlyCollection<Guid> courierIds, CancellationToken ct);
    Task<(int Total, int Available, int Unavailable, int ActiveDeliveries)> GetCourierSummaryAsync(CancellationToken ct);
    Task<(int Unassigned, int AvailableCouriers, int CouriersWithActiveDeliveries, int ActiveDeliveries, int TotalCouriers)> GetDashboardSummaryAsync(CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
    Task EnqueueOutboxEventAsync(Guid eventId, string routingKey, string payload, DateTimeOffset occurredAt, CancellationToken ct);
    Task<IReadOnlyList<DispatchOutboxRecord>> GetPendingOutboxAsync(int limit, CancellationToken ct);
    Task MarkOutboxProcessedAsync(Guid eventId, CancellationToken ct);
    Task RecordOutboxFailureAsync(Guid eventId, CancellationToken ct);
    Task AddFromEventAsync(Guid eventId, ParcelAssignment assignment, CancellationToken ct);
    Task ApplyCourierAssignedAsync(Guid eventId, Guid parcelId, string trackingNumber, Guid courierId,
        DateTimeOffset assignedAtUtc, CancellationToken ct);
    Task RecordCourierAssignmentAsync(Guid parcelId, Guid courierId, Guid assignedBy,
        DateTimeOffset assignedAtUtc, CancellationToken ct);
    Task ApplyParcelStatusChangedAsync(ParcelStatusChangedMessage message, CancellationToken ct);
    Task ApplyUserRoleChangedAsync(Guid eventId, Guid userId, string role, CancellationToken ct);
    Task ApplyUserStatusChangedAsync(Guid eventId, Guid userId, bool isActive, CancellationToken ct);
}

internal enum CourierProfileDeleteStatus { Deleted, NotFound, HasActiveDeliveries, ConcurrencyConflict }
