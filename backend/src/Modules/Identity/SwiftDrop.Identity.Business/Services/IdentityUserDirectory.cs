using SwiftDrop.Identity.Contracts;
using SwiftDrop.Identity.Data.Repositories;
using SwiftDrop.SharedKernel.Authorization;

namespace SwiftDrop.Identity.Business.Services;

internal sealed class IdentityUserDirectory(IUserRepository users) : IIdentityUserDirectory
{
    public async Task<bool> IsActiveCourierAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(userId, ct);
        return user is { IsActive: true } && string.Equals(user.Role.Name, Roles.Courier, StringComparison.OrdinalIgnoreCase);
    }

    public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct) =>
        users.GetNamesAsync(userIds, ct);
}
