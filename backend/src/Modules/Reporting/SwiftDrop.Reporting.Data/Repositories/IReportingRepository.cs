using SwiftDrop.Reporting.Data.Entities;

namespace SwiftDrop.Reporting.Data.Repositories;

internal interface IReportingRepository
{
    Task ApplyEventAsync(Guid eventId, DateOnly reportDate, int createdIncrement,
        int deliveredIncrement, int failedIncrement, CancellationToken ct);
    Task<IReadOnlyList<DailyStatistic>> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken ct);
    Task<DailyStatistic?> GetForDateAsync(DateOnly reportDate, CancellationToken ct);
}
