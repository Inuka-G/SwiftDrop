using SwiftDrop.Reporting.Contracts;
using SwiftDrop.Reporting.Data.Repositories;

namespace SwiftDrop.Reporting.Business.Services;

internal sealed class ReportService(IReportingRepository repository, IReportDateResolver dateResolver, TimeProvider timeProvider) : IReportService
{
    public async Task<ReportResult<DeliveriesPerDayResponse>> GetDeliveriesPerDayAsync(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        if (!TryRange(from, to, out var start, out var end)) return ReportResult<DeliveriesPerDayResponse>.Fail(ReportFailure.InvalidDateRange);
        var stats = await repository.GetRangeAsync(start, end, ct);
        var byDate = stats.ToDictionary(x => x.ReportDate);
        var days = Enumerable.Range(0, end.DayNumber - start.DayNumber + 1).Select(offset =>
        {
            var date = start.AddDays(offset);
            byDate.TryGetValue(date, out var item);
            return new DailyDeliveryCountDto(date, item?.CreatedCount ?? 0, item?.DeliveredCount ?? 0, item?.FailedCount ?? 0);
        }).ToArray();
        return ReportResult<DeliveriesPerDayResponse>.Success(new(start, end, days));
    }

    public async Task<ReportResult<SuccessFailureRateResponse>> GetSuccessFailureRateAsync(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        if (!TryRange(from, to, out var start, out var end)) return ReportResult<SuccessFailureRateResponse>.Fail(ReportFailure.InvalidDateRange);
        var stats = await repository.GetRangeAsync(start, end, ct);
        var delivered = stats.Sum(x => x.DeliveredCount);
        var failed = stats.Sum(x => x.FailedCount);
        var total = delivered + failed;
        return ReportResult<SuccessFailureRateResponse>.Success(new(start, end, delivered, failed, total,
            total == 0 ? null : (decimal)delivered / total));
    }

    public async Task<ReportingSummaryResponse> GetSummaryAsync(CancellationToken ct)
    {
        var date = dateResolver.FromUtc(timeProvider.GetUtcNow());
        var item = await repository.GetForDateAsync(date, ct);
        var delivered = item?.DeliveredCount ?? 0;
        var failed = item?.FailedCount ?? 0;
        var total = delivered + failed;
        return new(date, item?.CreatedCount ?? 0, delivered, failed, total == 0 ? null : (decimal)delivered / total);
    }

    private static bool TryRange(DateOnly? from, DateOnly? to, out DateOnly start, out DateOnly end)
    {
        start = from.GetValueOrDefault();
        end = to.GetValueOrDefault();
        return from.HasValue && to.HasValue && start <= end && end.DayNumber - start.DayNumber + 1 <= ReportQueryLimits.MaximumRangeDays;
    }
}
