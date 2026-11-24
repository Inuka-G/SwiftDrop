using System.Text.Json;
using SwiftDrop.Dispatch.Data.Repositories;
using SwiftDrop.Identity.Contracts;

namespace SwiftDrop.Dispatch.Business.Services;

/// Dispatch-owned adapter for Identity's public event contract. Events are staged in
/// the Dispatch outbox so Identity's post-save notification survives broker outages.
internal sealed class DispatchIdentityEventPublisher(IDispatchRepository repository) : IIdentityEventPublisher
{
    public Task PublishAsync(UserRoleChanged message, CancellationToken ct) =>
        repository.EnqueueOutboxEventAsync(message.EventId, "identity.user-role-changed",
            JsonSerializer.Serialize(message, JsonSerializerOptions.Web), message.ChangedAtUtc, ct);

    public Task PublishAsync(UserStatusChanged message, CancellationToken ct) =>
        repository.EnqueueOutboxEventAsync(message.EventId, "identity.user-status-changed",
            JsonSerializer.Serialize(message, JsonSerializerOptions.Web), message.ChangedAtUtc, ct);
}
