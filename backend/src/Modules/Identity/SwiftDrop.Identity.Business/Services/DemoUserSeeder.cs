using Microsoft.AspNetCore.Identity;
using SwiftDrop.Identity.Data.Entities;
using SwiftDrop.Identity.Data.Repositories;
using SwiftDrop.SharedKernel.Authorization;

namespace SwiftDrop.Identity.Business.Services;

/// Creates the four demo logins listed in the design report (section 10).
/// These passwords are public, so this only ever runs in the Development environment.
internal static class DemoUserSeeder
{
    private static readonly (Guid Id, string Username, string FullName, string RoleName)[] DemoUsers =
    [
        (Guid.Parse("9D32AD04-30D6-463A-AB18-C6A01B3641A6"), "admin", "Demo Admin", Roles.Admin),
        (Guid.Parse("4A0C79E5-C347-4F1A-85DD-EB7F14D0A94A"), "customer1", "Demo Customer", Roles.Customer),
        (Guid.Parse("9B002EB9-C2BC-48CA-A018-909293E3A83E"), "courier1", "Demo Courier", Roles.Courier),
        (Guid.Parse("58363BA0-04AC-4F99-9C7B-279C87ABAF4A"), "merchant1", "Demo Merchant", Roles.Merchant),
    ];

    public static async Task SeedAsync(IUserRepository users, IPasswordHasher<User> hasher, CancellationToken ct)
    {
        foreach (var (id, username, fullName, roleName) in DemoUsers)
        {
            if (await users.FindByLoginAsync(username, ct) is not null) continue;

            var role = await users.GetRoleAsync(roleName, ct)
                ?? throw new InvalidOperationException($"The {roleName} role is missing from the roles table.");

            var user = new User(id, fullName, $"{username}@swiftdrop.local", username, phoneNumber: null, role);
            user.SetPasswordHash(hasher.HashPassword(user, username));   // the report's password equals the username
            await users.AddAsync(user, ct);
        }

        await users.SaveChangesAsync(ct);
    }
}
