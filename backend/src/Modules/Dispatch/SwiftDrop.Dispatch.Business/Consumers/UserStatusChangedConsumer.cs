using SwiftDrop.Dispatch.Contracts;
using SwiftDrop.Dispatch.Data.Repositories;

namespace SwiftDrop.Dispatch.Business.Consumers;

internal sealed class UserStatusChangedConsumer(IDispatchRepository repository)
{
    public Task HandleAsync(UserStatusChangedMessage message, CancellationToken ct) =>
        repository.ApplyUserStatusChangedAsync(message.EventId, message.UserId, message.IsActive, ct);
}
