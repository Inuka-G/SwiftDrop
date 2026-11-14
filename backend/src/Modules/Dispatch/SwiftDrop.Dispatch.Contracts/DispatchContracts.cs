namespace SwiftDrop.Dispatch.Contracts;

public sealed record UnassignedParcelDto(Guid ParcelId, string TrackingNumber, Guid ZoneId, DateTimeOffset CreatedAt);
public sealed record UnassignedParcelPage(IReadOnlyList<UnassignedParcelDto> Items, int Page, int PageSize, int TotalCount);
public sealed record AssignCourierCommand(Guid CourierId, Guid AssignedBy);
public sealed record AssignmentDto(Guid ParcelId, Guid CourierId, Guid AssignedBy, DateTimeOffset AssignedAt);

public enum DispatchFailure { ParcelNotFound, ParcelNotAssignable, CourierNotEligible, ConcurrencyConflict,
    CourierAlreadyRegistered, CourierProfileNotFound, ZoneNotFound }
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
public sealed record CourierProfileDto(Guid CourierId, string Name, Guid? ZoneId, bool IsAvailable,
    string? VehicleType, string? LicensePlate, int ActiveDeliveryCount);
public sealed record CourierProfilePage(IReadOnlyList<CourierProfileDto> Items, int Page, int PageSize, int TotalCount);
public sealed record CourierDashboardSummary(int TotalCouriers, int AvailableCouriers,
    int UnavailableCouriers, int ActiveDeliveries);
public sealed record DispatchDashboardSummary(int UnassignedCount, int AvailableCouriers,
    int CouriersWithActiveDeliveries, int ActiveDeliveries, int TotalCouriers);
public sealed record CourierProfileResult(CourierProfileDto? Profile, DispatchFailure? Failure)
{
    public bool IsSuccess => Failure is null;
    public static CourierProfileResult Success(CourierProfileDto profile) => new(profile, null);
    public static CourierProfileResult Fail(DispatchFailure failure) => new(null, failure);
}

public interface IDispatchService
{
    Task<UnassignedParcelPage> ListUnassignedAsync(Guid? zoneId, DateTimeOffset? from, DateTimeOffset? to,
        int page, int pageSize, CancellationToken ct);
    Task<DispatchResult<AssignmentDto>> AssignAsync(Guid parcelId, Guid courierId, Guid assignedBy, CancellationToken ct);
    Task<CourierProfileResult> RegisterCourierAsync(Guid courierId, Guid? zoneId, string? vehicleType,
        string? licensePlate, CancellationToken ct);
    Task<CourierProfileResult> UpdateCourierAsync(Guid courierId, Guid? zoneId, bool? isAvailable,
        string? vehicleType, string? licensePlate, CancellationToken ct);
    Task<CourierProfilePage> ListCouriersAsync(Guid? zoneId, bool? isAvailable, int page, int pageSize, CancellationToken ct);
    Task<CourierDashboardSummary> GetCourierSummaryAsync(CancellationToken ct);
    Task<DispatchDashboardSummary> GetDashboardSummaryAsync(CancellationToken ct);
}
