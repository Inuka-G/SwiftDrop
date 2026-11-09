using SwiftDrop.Identity.Contracts;

namespace SwiftDrop.Identity.Business.Services;

/// The repository has no SharedKernel/RabbitMQ publisher yet. This adapter keeps the dependency explicit.
internal sealed class UnconfiguredIdentityEventPublisher : IIdentityEventPublisher
{
    public Task PublishAsync(UserRoleChanged message, CancellationToken ct) =>
        Task.FromException(new InvalidOperationException("Identity event publishing is not configured."));

    public Task PublishAsync(UserStatusChanged message, CancellationToken ct) =>
        Task.FromException(new InvalidOperationException("Identity event publishing is not configured."));
}
