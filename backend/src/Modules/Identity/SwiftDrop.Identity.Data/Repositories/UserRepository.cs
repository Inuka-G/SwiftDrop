using Microsoft.EntityFrameworkCore;
using SwiftDrop.Identity.Data.Entities;
using SwiftDrop.Identity.Data.Persistence;

namespace SwiftDrop.Identity.Data.Repositories;

internal sealed class UserRepository(IdentityDbContext db) : IUserRepository
{
    public Task<User?> FindByLoginAsync(string login, CancellationToken ct) =>
        db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == login || u.Username == login, ct);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Role?> GetRoleAsync(string roleName, CancellationToken ct) =>
        db.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct);

    public async Task AddAsync(User user, CancellationToken ct) =>
        await db.Users.AddAsync(user, ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
