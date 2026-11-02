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
    public static readonly Role AdminRole = new(5, Roles.Admin);

    private static readonly Role[] AllRoles = [CustomerRole, MerchantRole, CourierRole, AdminRole];

    public List<User> Users { get; } = [];
    public int SaveCount { get; private set; }

    public Task<User?> FindByLoginAsync(string login, CancellationToken ct) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Email == login || u.Username == login));

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

    public Task<Role?> GetRoleAsync(string roleName, CancellationToken ct) =>
        Task.FromResult(AllRoles.FirstOrDefault(r => r.Name == roleName));

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
}
