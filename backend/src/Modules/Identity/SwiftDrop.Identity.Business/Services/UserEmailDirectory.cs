using SwiftDrop.Identity.Contracts;
using SwiftDrop.Identity.Data.Repositories;

namespace SwiftDrop.Identity.Business.Services;

internal sealed class UserEmailDirectory(IUserRepository repository) : IUserEmailDirectory
{
    public async Task<string?> GetActiveEmailAsync(Guid id, CancellationToken ct)
    {
        var user = await repository.GetByIdAsync(id, ct);
        return user is { IsActive: true } ? user.Email : null;
    }
}
