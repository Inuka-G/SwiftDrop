using SwiftDrop.Reporting.Data.Repositories;

namespace SwiftDrop.Reporting.Business.Consumers;

internal sealed record ParcelStatusChangedReceived(Guid EventId, DateTimeOffset ChangedAtUtc, string NewStatus);

internal sealed class ParcelStatusChangedConsumer(IReportingRepository repository, Services.IReportDateResolver dateResolver)
{
    public Task HandleAsync(ParcelStatusChangedReceived message, CancellationToken ct)
    {
        var delivered = string.Equals(message.NewStatus, "DELIVERED", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        var failed = string.Equals(message.NewStatus, "FAILED", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        return repository.ApplyEventAsync(message.EventId, dateResolver.FromUtc(message.ChangedAtUtc), 0, delivered, failed, ct);
    }
}
