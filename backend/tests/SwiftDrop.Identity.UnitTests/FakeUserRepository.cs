using SwiftDrop.Identity.Data.Entities;
using SwiftDrop.Identity.Data.Repositories;
using SwiftDrop.SharedKernel.Authorization;

namespace SwiftDrop.Identity.UnitTests;

/// An in-memory stand-in for the database, so these tests run fast and need no setup.
internal sealed class FakeUserRepository : IUserRepository
{
    public static readonly Role CustomerRole = new(1, Roles.Customer);
    public static readonly Role MerchantRole = new(2, Roles.Merchant);
    public static readonly Role CourierRole = new(3, Roles.Courier);
    public static readonly Role AdminRole = new(4, Roles.Admin);

    private static readonly Role[] AllRoles = [CustomerRole, MerchantRole, CourierRole, AdminRole];

    public List<User> Users { get; } = [];
    public int SaveCount { get; private set; }

    public Task<User?> FindByLoginAsync(string login, CancellationToken ct) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Email == login || u.Username == login));

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

    public Task<User?> FindByProviderAsync(string provider, string providerUserId, CancellationToken ct) =>
        Task.FromResult(Users.FirstOrDefault(u => u.AuthProvider == provider && u.ProviderUserId == providerUserId));

    public Task<Role?> GetRoleAsync(string roleName, CancellationToken ct) =>
        Task.FromResult(AllRoles.FirstOrDefault(r => r.Name == roleName));

    public Task<(IReadOnlyList<User> Users, int TotalCount)> ListUsersAsync(string? search, string? role,
        bool? isActive, int page, int pageSize, CancellationToken ct)
    {
        IEnumerable<User> query = Users;
        if (!string.IsNullOrWhiteSpace(role)) query = query.Where(user => user.Role.Name == role);
        if (isActive.HasValue) query = query.Where(user => user.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(user =>
            user.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            user.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
        var matched = query.OrderByDescending(user => user.CreatedAtUtc).ThenBy(user => user.Id).ToArray();
        return Task.FromResult(((IReadOnlyList<User>)matched.Skip((int)Math.Min((long)(page - 1) * pageSize, int.MaxValue))
            .Take(pageSize).ToArray(), matched.Length));
    }

    public Task<int> CountActiveUsersInRoleAsync(string roleName, CancellationToken ct) =>
        Task.FromResult(Users.Count(user => user.IsActive && user.Role.Name == roleName));

    public Task AddAsync(User user, CancellationToken ct)
    {
        Users.Add(user);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct) => operation(ct);
}
