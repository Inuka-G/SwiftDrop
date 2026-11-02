using Microsoft.AspNetCore.Identity;
using SwiftDrop.Identity.Business.Services;
using SwiftDrop.Identity.Data.Entities;
using SwiftDrop.SharedKernel.Authorization;

namespace SwiftDrop.Identity.UnitTests;

public class DemoUserSeederTests
{
    [Theory]
    [InlineData("admin", Roles.Admin)]
    [InlineData("customer1", Roles.Customer)]
    [InlineData("courier1", Roles.Courier)]
    [InlineData("merchant1", Roles.Merchant)]
    public async Task Seeds_each_report_login_with_its_role_and_password(string username, string role)
    {
        var users = new FakeUserRepository();
        var hasher = new PasswordHasher<User>();

        await DemoUserSeeder.SeedAsync(users, hasher, default);

        var user = Assert.Single(users.Users, u => u.Username == username);
        Assert.Equal(role, user.Role.Name);
        Assert.NotEqual(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(user, user.PasswordHash!, username));
    }

    [Fact]
    public async Task Running_twice_does_not_create_duplicates()
    {
        var users = new FakeUserRepository();
        var hasher = new PasswordHasher<User>();

        await DemoUserSeeder.SeedAsync(users, hasher, default);
        await DemoUserSeeder.SeedAsync(users, hasher, default);

        Assert.Equal(4, users.Users.Count);
    }
}
