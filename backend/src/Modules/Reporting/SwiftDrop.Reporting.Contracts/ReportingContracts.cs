namespace SwiftDrop.Reporting.Contracts;

public static class ReportQueryLimits
{
    public const int MaximumRangeDays = 366;
}

public sealed record DailyDeliveryCountDto(DateOnly Date, int CreatedCount, int DeliveredCount, int FailedCount);
public sealed record DeliveriesPerDayResponse(DateOnly From, DateOnly To, IReadOnlyList<DailyDeliveryCountDto> Days);
public sealed record SuccessFailureRateResponse(DateOnly From, DateOnly To, int DeliveredCount,
    int FailedCount, int TotalCount, decimal? SuccessRate);
public sealed record ReportingSummaryResponse(DateOnly Date, int CreatedCount, int DeliveredCount,
    int FailedCount, decimal? SuccessRate);

public enum ReportFailure { InvalidDateRange }

public sealed record ReportResult<T>(T? Value, ReportFailure? Failure) where T : class
{
    public bool IsSuccess => Failure is null;
    public static ReportResult<T> Success(T value) => new(value, null);
    public static ReportResult<T> Fail(ReportFailure failure) => new(null, failure);
}

public interface IReportService
{
    Task<ReportResult<DeliveriesPerDayResponse>> GetDeliveriesPerDayAsync(DateOnly? from, DateOnly? to, CancellationToken ct);
    Task<ReportResult<SuccessFailureRateResponse>> GetSuccessFailureRateAsync(DateOnly? from, DateOnly? to, CancellationToken ct);
    Task<ReportingSummaryResponse> GetSummaryAsync(CancellationToken ct);
}
