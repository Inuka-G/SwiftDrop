using SwiftDrop.Dispatch.Contracts;
using SwiftDrop.Dispatch.Data.Repositories;

namespace SwiftDrop.Dispatch.Business.Consumers;

internal sealed class UserRoleChangedConsumer(IDispatchRepository repository)
{
    public Task HandleAsync(UserRoleChangedMessage message, CancellationToken ct) =>
        repository.ApplyUserRoleChangedAsync(message.EventId, message.UserId, message.Role, ct);
}
