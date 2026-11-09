using SwiftDrop.Reporting.Data.Repositories;

namespace SwiftDrop.Reporting.Business.Consumers;

internal sealed record ParcelCreatedReceived(Guid EventId, DateTimeOffset CreatedAtUtc);

internal sealed class ParcelCreatedConsumer(IReportingRepository repository, Services.IReportDateResolver dateResolver)
{
    public Task HandleAsync(ParcelCreatedReceived message, CancellationToken ct) =>
        repository.ApplyEventAsync(message.EventId, dateResolver.FromUtc(message.CreatedAtUtc), 1, 0, 0, ct);
}
