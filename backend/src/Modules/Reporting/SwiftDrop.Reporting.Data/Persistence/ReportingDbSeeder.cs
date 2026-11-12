using Microsoft.EntityFrameworkCore;
using SwiftDrop.Reporting.Data.Entities;

namespace SwiftDrop.Reporting.Data.Persistence;

internal static class ReportingDbSeeder
{
    public static async Task SeedSampleDataAsync(ReportingDbContext db, CancellationToken ct)
    {
        if (await db.DailyStatistics.AnyAsync(ct)) return;

        // Synthetic chart data only; never mixed in once real event counts exist.
        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        db.DailyStatistics.AddRange(Enumerable.Range(0, 30).Select(offset =>
        {
            var created = 8 + (offset % 7);
            var failed = offset % 3;
            return new DailyStatistic
            {
                ReportDate = todayUtc.AddDays(offset - 29),
                CreatedCount = created,
                DeliveredCount = created - failed,
                FailedCount = failed
            };
        }));
        await db.SaveChangesAsync(ct);
    }
}
