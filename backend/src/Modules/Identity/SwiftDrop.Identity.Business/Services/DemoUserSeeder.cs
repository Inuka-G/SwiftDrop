using Microsoft.AspNetCore.Identity;
using SwiftDrop.Identity.Data.Entities;
using SwiftDrop.Identity.Data.Repositories;
using SwiftDrop.SharedKernel.Authorization;

namespace SwiftDrop.Identity.Business.Services;

/// Creates the four demo logins listed in the design report (section 10).
/// These passwords are public, so this only ever runs in the Development environment.
internal static class DemoUserSeeder
{
    private static readonly (string Username, string FullName, string RoleName)[] DemoUsers =
    [
        ("admin", "Demo Admin", Roles.Admin),
        ("customer1", "Demo Customer", Roles.Customer),
        ("courier1", "Demo Courier", Roles.Courier),
        ("merchant1", "Demo Merchant", Roles.Merchant),
    ];

    public static async Task SeedAsync(IUserRepository users, IPasswordHasher<User> hasher, CancellationToken ct)
    {
        foreach (var (username, fullName, roleName) in DemoUsers)
        {
            if (await users.FindByLoginAsync(username, ct) is not null) continue;

            var role = await users.GetRoleAsync(roleName, ct)
                ?? throw new InvalidOperationException($"The {roleName} role is missing from the roles table.");

            var user = new User(fullName, $"{username}@swiftdrop.local", username, phoneNumber: null, role);
            user.SetPasswordHash(hasher.HashPassword(user, username));   // the report's password equals the username
            await users.AddAsync(user, ct);
        }

        await users.SaveChangesAsync(ct);
    }
}
