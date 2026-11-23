namespace SwiftDrop.Dispatch.Business.Services;

// Host-safe placeholders for integrations that do not have a public module contract yet.
internal sealed class UnconfiguredDispatchAdapters : IDispatchEventPublisher, IZoneLookup
{
    private static InvalidOperationException Missing(string contract) =>
        new($"Dispatch adapter is not configured: {contract}.");

    public Task<bool> ExistsAsync(Guid zoneId, CancellationToken ct) =>
        throw Missing("Pricing zone lookup");

    public Task PublishAsync(SwiftDrop.Dispatch.Contracts.CourierAssignedEvent message, CancellationToken ct) =>
        throw Missing("SharedKernel/RabbitMQ event publisher");
}
