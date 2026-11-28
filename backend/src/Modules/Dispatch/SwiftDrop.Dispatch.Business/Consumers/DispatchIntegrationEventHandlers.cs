using SwiftDrop.Dispatch.Business.Consumers;
using SwiftDrop.Dispatch.Contracts;
using SwiftDrop.Dispatch.Data.Repositories;
using SwiftDrop.SharedKernel.Messaging;
using ParcelCreatedEvent = SwiftDrop.Parcels.Contracts.Events.ParcelCreatedIntegrationEvent;
using ParcelStatusEvent = SwiftDrop.Parcels.Contracts.Events.ParcelStatusChangedIntegrationEvent;
using CourierAssignedEvent = SwiftDrop.Parcels.Contracts.Events.CourierAssignedIntegrationEvent;

namespace SwiftDrop.Dispatch.Business;

internal sealed class ParcelCreatedEventHandler(ParcelCreatedConsumer consumer)
    : IIntegrationEventHandler<ParcelCreatedEvent>
{
    public Task HandleAsync(ParcelCreatedEvent evt, CancellationToken ct) =>
        consumer.HandleAsync(new ParcelCreatedMessage(evt.EventId, evt.ParcelId, evt.TrackingNumber,
            evt.ZoneId, new DateTimeOffset(DateTime.SpecifyKind(evt.OccurredAtUtc, DateTimeKind.Utc))), ct);
}

internal sealed class ParcelStatusChangedEventHandler(ParcelStatusChangedConsumer consumer)
    : IIntegrationEventHandler<ParcelStatusEvent>
{
    public Task HandleAsync(ParcelStatusEvent evt, CancellationToken ct) =>
        consumer.HandleAsync(new ParcelStatusChangedMessage(evt.EventId, evt.ParcelId, evt.TrackingNumber,
            evt.CourierId, evt.NewStatus,
            new DateTimeOffset(DateTime.SpecifyKind(evt.OccurredAtUtc, DateTimeKind.Utc))), ct);
}

internal sealed class CourierAssignedEventHandler(IDispatchRepository repository)
    : IIntegrationEventHandler<CourierAssignedEvent>
{
    public Task HandleAsync(CourierAssignedEvent evt, CancellationToken ct) =>
        repository.ApplyCourierAssignedAsync(evt.EventId, evt.ParcelId, evt.TrackingNumber, evt.CourierId,
            new DateTimeOffset(DateTime.SpecifyKind(evt.AssignedAtUtc, DateTimeKind.Utc)), ct);
}

internal sealed class IdentityUserRoleChangedEventHandler(UserRoleChangedConsumer consumer)
    : IIntegrationEventHandler<SwiftDrop.Identity.Contracts.UserRoleChanged>
{
    public Task HandleAsync(SwiftDrop.Identity.Contracts.UserRoleChanged evt, CancellationToken ct) =>
        consumer.HandleAsync(new UserRoleChangedMessage(evt.EventId, evt.UserId, evt.NewRole), ct);
}

internal sealed class IdentityUserStatusChangedEventHandler(UserStatusChangedConsumer consumer)
    : IIntegrationEventHandler<SwiftDrop.Identity.Contracts.UserStatusChanged>
{
    public Task HandleAsync(SwiftDrop.Identity.Contracts.UserStatusChanged evt, CancellationToken ct) =>
        consumer.HandleAsync(new UserStatusChangedMessage(evt.EventId, evt.UserId, evt.IsActive), ct);
}
