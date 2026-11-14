using Microsoft.EntityFrameworkCore;
using System.Data;
using SwiftDrop.Identity.Data.Entities;
using SwiftDrop.Identity.Data.Persistence;

namespace SwiftDrop.Identity.Data.Repositories;

internal sealed class UserRepository(IdentityDbContext db) : IUserRepository
{
    public Task<User?> FindByLoginAsync(string login, CancellationToken ct) =>
        db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == login || u.Username == login, ct);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> FindByProviderAsync(string provider, string providerUserId, CancellationToken ct) =>
        db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.AuthProvider == provider && u.ProviderUserId == providerUserId, ct);

    public Task<Role?> GetRoleAsync(string roleName, CancellationToken ct) =>
        db.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct);

    public async Task<(IReadOnlyList<User> Users, int TotalCount)> ListUsersAsync(string? search, string? role,
        bool? isActive, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Users.AsNoTracking().Include(u => u.Role).AsQueryable();
        if (!string.IsNullOrWhiteSpace(role)) query = query.Where(u => u.Role.Name == role);
        if (isActive.HasValue) query = query.Where(u => u.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var escaped = EscapeLikePattern(search.Trim().ToLowerInvariant());
            var pattern = $"%{escaped}%";
            query = query.Where(u => EF.Functions.Like(u.FullName.ToLower(), pattern, "\\") ||
                                     EF.Functions.Like(u.Email.ToLower(), pattern, "\\"));
        }

        var total = await query.CountAsync(ct);
        var offset = (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue);
        var users = await query.OrderByDescending(u => u.CreatedAtUtc).ThenBy(u => u.Id)
            .Skip(offset).Take(pageSize).ToListAsync(ct);
        return (users, total);
    }

    public Task<int> CountActiveUsersInRoleAsync(string roleName, CancellationToken ct) =>
        db.Users.CountAsync(u => u.IsActive && u.Role.Name == roleName, ct);

    public async Task AddAsync(User user, CancellationToken ct) =>
        await db.Users.AddAsync(user, ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var result = await operation(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static string EscapeLikePattern(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);
}
