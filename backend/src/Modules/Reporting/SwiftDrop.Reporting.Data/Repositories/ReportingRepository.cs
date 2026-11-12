using Microsoft.EntityFrameworkCore;
using SwiftDrop.Reporting.Data.Entities;
using SwiftDrop.Reporting.Data.Persistence;

namespace SwiftDrop.Reporting.Data.Repositories;

internal sealed class ReportingRepository(ReportingDbContext db) : IReportingRepository
{
    public async Task ApplyEventAsync(Guid eventId, DateOnly reportDate, int createdIncrement,
        int deliveredIncrement, int failedIncrement, CancellationToken ct)
    {
        // A conflicting first insert or duplicate event rolls back both the count and processed marker.
        // Retry from a fresh tracker so concurrent first events for one date are not lost.
        for (var attempt = 0; ; attempt++)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            try
            {
                if (await db.ProcessedEvents.AnyAsync(x => x.EventId == eventId, ct)) return;

                var hasCountUpdate = createdIncrement != 0 || deliveredIncrement != 0 || failedIncrement != 0;
                if (hasCountUpdate)
                {
                    if (!await db.DailyStatistics.AnyAsync(x => x.ReportDate == reportDate, ct))
                    {
                        db.DailyStatistics.Add(new DailyStatistic { ReportDate = reportDate });
                        await db.SaveChangesAsync(ct);
                    }

                    var updated = await db.DailyStatistics.Where(x => x.ReportDate == reportDate)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(x => x.CreatedCount, x => x.CreatedCount + createdIncrement)
                            .SetProperty(x => x.DeliveredCount, x => x.DeliveredCount + deliveredIncrement)
                            .SetProperty(x => x.FailedCount, x => x.FailedCount + failedIncrement), ct);
                    if (updated != 1) throw new InvalidOperationException("Daily statistic row was not available for update.");
                }

                db.ProcessedEvents.Add(new ProcessedEvent { EventId = eventId, ProcessedAtUtc = DateTimeOffset.UtcNow });
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return;
            }
            catch (DbUpdateException) when (attempt < 2)
            {
                await transaction.RollbackAsync(ct);
                db.ChangeTracker.Clear();
            }
        }
    }

    public async Task<IReadOnlyList<DailyStatistic>> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken ct) =>
        await db.DailyStatistics.AsNoTracking().Where(x => x.ReportDate >= from && x.ReportDate <= to)
            .OrderBy(x => x.ReportDate).ToListAsync(ct);

    public Task<DailyStatistic?> GetForDateAsync(DateOnly reportDate, CancellationToken ct) =>
        db.DailyStatistics.AsNoTracking().SingleOrDefaultAsync(x => x.ReportDate == reportDate, ct);
}
