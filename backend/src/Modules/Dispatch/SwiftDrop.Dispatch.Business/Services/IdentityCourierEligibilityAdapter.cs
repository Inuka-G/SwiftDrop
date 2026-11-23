using SwiftDrop.Identity.Contracts;

namespace SwiftDrop.Dispatch.Business.Services;

internal sealed class IdentityCourierEligibilityAdapter(IIdentityUserDirectory users) : ICourierEligibilityGateway
{
    public Task<bool> IsEligibleAsync(Guid courierId, CancellationToken ct) =>
        users.IsActiveCourierAsync(courierId, ct);

    public Task<bool> IsActiveCourierAsync(Guid userId, CancellationToken ct) =>
        users.IsActiveCourierAsync(userId, ct);

    public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct) =>
        users.GetNamesAsync(userIds, ct);
}
