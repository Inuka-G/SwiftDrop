using SwiftDrop.Identity.Data.Entities;

namespace SwiftDrop.Identity.Data.Repositories;

public interface IUserRepository
{
    /// Finds a user whose email or username equals the given (lower-case) login.
    Task<User?> FindByLoginAsync(string login, CancellationToken ct);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Role?> GetRoleAsync(string roleName, CancellationToken ct);
    Task<(IReadOnlyList<User> Users, int TotalCount)> ListUsersAsync(string? search, string? role,
        bool? isActive, int page, int pageSize, CancellationToken ct);
    Task<int> CountActiveUsersInRoleAsync(string roleName, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct);
}
