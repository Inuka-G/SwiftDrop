namespace SwiftDrop.Reporting.Business.Services;

internal interface IReportDateResolver
{
    DateOnly FromUtc(DateTimeOffset timestamp);
}

internal sealed class ReportDateResolver : IReportDateResolver
{
    public DateOnly FromUtc(DateTimeOffset timestamp) => DateOnly.FromDateTime(timestamp.UtcDateTime);
}
