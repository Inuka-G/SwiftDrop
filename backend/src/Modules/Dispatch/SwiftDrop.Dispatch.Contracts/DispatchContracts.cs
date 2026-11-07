namespace SwiftDrop.Dispatch.Contracts;

public sealed record UnassignedParcelDto(Guid ParcelId, string TrackingNumber, Guid ZoneId, DateTimeOffset CreatedAt);
public sealed record UnassignedParcelPage(IReadOnlyList<UnassignedParcelDto> Items, int Page, int PageSize, int TotalCount);
public sealed record AssignCourierCommand(Guid CourierId, Guid AssignedBy);
public sealed record AssignmentDto(Guid ParcelId, Guid CourierId, Guid AssignedBy, DateTimeOffset AssignedAt);

public enum DispatchFailure { ParcelNotFound, ParcelNotAssignable, CourierNotEligible, ConcurrencyConflict }
public sealed record DispatchResult<T>(T? Value, DispatchFailure? Failure)
{
    public bool IsSuccess => Failure is null;
    public static DispatchResult<T> Success(T value) => new(value, null);
    public static DispatchResult<T> Fail(DispatchFailure failure) => new(default, failure);
}

// SharedKernel currently has no integration-event base class. This immutable event is
// kept in Dispatch.Contracts until the shared messaging contract is available.
public sealed record CourierAssignedEvent(Guid EventId, Guid ParcelId, Guid CourierId,
    string TrackingNumber, DateTimeOffset OccurredAt);

public interface IDispatchService
{
    Task<UnassignedParcelPage> ListUnassignedAsync(Guid? zoneId, DateTimeOffset? from, DateTimeOffset? to,
        int page, int pageSize, CancellationToken ct);
    Task<DispatchResult<AssignmentDto>> AssignAsync(Guid parcelId, Guid courierId, Guid assignedBy, CancellationToken ct);
}
