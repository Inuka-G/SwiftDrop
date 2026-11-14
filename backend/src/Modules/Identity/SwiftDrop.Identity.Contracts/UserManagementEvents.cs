namespace SwiftDrop.Identity.Contracts;

/// Identity-owned event contracts. SharedKernel currently has no integration-event base or publisher abstraction.
public sealed record UserRoleChanged(Guid EventId, Guid UserId, string PreviousRole, string NewRole,
    DateTimeOffset ChangedAtUtc);

public sealed record UserStatusChanged(Guid EventId, Guid UserId, bool IsActive, DateTimeOffset ChangedAtUtc);

public interface IIdentityEventPublisher
{
    Task PublishAsync(UserRoleChanged message, CancellationToken ct);
    Task PublishAsync(UserStatusChanged message, CancellationToken ct);
}
