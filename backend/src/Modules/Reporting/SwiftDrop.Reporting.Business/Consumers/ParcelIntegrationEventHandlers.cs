using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.Reporting.Business.Consumers;
using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.Reporting.Business;

internal sealed class ParcelCreatedReportHandler(ParcelCreatedConsumer consumer)
    : IIntegrationEventHandler<ParcelCreatedIntegrationEvent>
{
    public Task HandleAsync(ParcelCreatedIntegrationEvent evt, CancellationToken ct) =>
        consumer.HandleAsync(new ParcelCreatedReceived(evt.EventId,
            new DateTimeOffset(DateTime.SpecifyKind(evt.OccurredAtUtc, DateTimeKind.Utc))), ct);
}

internal sealed class ParcelStatusChangedReportHandler(ParcelStatusChangedConsumer consumer)
    : IIntegrationEventHandler<ParcelStatusChangedIntegrationEvent>
{
    public Task HandleAsync(ParcelStatusChangedIntegrationEvent evt, CancellationToken ct) =>
        consumer.HandleAsync(new ParcelStatusChangedReceived(evt.EventId,
            new DateTimeOffset(DateTime.SpecifyKind(evt.OccurredAtUtc, DateTimeKind.Utc)), evt.NewStatus), ct);
}
