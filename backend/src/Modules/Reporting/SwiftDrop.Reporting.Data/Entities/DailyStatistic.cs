namespace SwiftDrop.Reporting.Data.Entities;

internal sealed class DailyStatistic
{
    public DateOnly ReportDate { get; set; }
    public int CreatedCount { get; set; }
    public int DeliveredCount { get; set; }
    public int FailedCount { get; set; }
}
