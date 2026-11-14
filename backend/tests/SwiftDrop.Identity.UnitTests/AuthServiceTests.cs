using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SwiftDrop.Identity.Business.Services;
using SwiftDrop.Identity.Data.Entities;
using SwiftDrop.SharedKernel.Authorization;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Identity.UnitTests;

public class AuthServiceTests
{
    private readonly FakeUserRepository _users = new();
    private readonly PasswordHasher<User> _hasher = new();
    private readonly AuthService _auth;

    public AuthServiceTests()
    {
        var jwt = Options.Create(new JwtOptions { Key = new string('k', JwtOptions.MinimumKeyBytes) });
        var codes = new GoogleLoginCodeStore(new MemoryCache(new MemoryCacheOptions()));
        _auth = new AuthService(_users, _hasher, new JwtTokenService(jwt), codes);
    }

    private User AddUser(string username, string password, Role role)
    {
        var user = new User("Test User", $"{username}@test.local", username, null, role);
        user.SetPasswordHash(_hasher.HashPassword(user, password));
        _users.Users.Add(user);
        return user;
    }

    [Fact]
    public async Task Login_with_correct_password_returns_token_and_role()
    {
        AddUser("courier1", "courier1", FakeUserRepository.CourierRole);

        var result = await _auth.LoginAsync(new LoginCommand("courier1", "courier1"), default);

        Assert.False(string.IsNullOrEmpty(result.AccessToken));
        Assert.Equal(Roles.Courier, result.User.Role);
    }

    [Fact]
    public async Task Login_also_accepts_email_in_any_letter_case()
    {
        AddUser("courier1", "courier1", FakeUserRepository.CourierRole);

        var result = await _auth.LoginAsync(new LoginCommand(" COURIER1@test.local ", "courier1"), default);

        Assert.Equal("courier1", result.User.Username);
    }

    [Theory]
    [InlineData("courier1", "wrong-password")]   // right user, wrong password
    [InlineData("nobody", "courier1")]           // user does not exist
    public async Task Login_failures_all_give_the_same_error(string username, string password)
    {
        AddUser("courier1", "courier1", FakeUserRepository.CourierRole);

        var error = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _auth.LoginAsync(new LoginCommand(username, password), default));

        Assert.Equal("Incorrect username or password.", error.Message);
    }

    [Fact]
    public async Task Login_is_refused_for_a_deactivated_user()
    {
        AddUser("courier1", "courier1", FakeUserRepository.CourierRole).Deactivate();

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _auth.LoginAsync(new LoginCommand("courier1", "courier1"), default));
    }

    [Fact]
    public async Task Login_is_refused_for_a_user_with_no_password()
    {
        _users.Users.Add(new User("Google User", "g@test.local", null, null, FakeUserRepository.CustomerRole));

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _auth.LoginAsync(new LoginCommand("g@test.local", "anything"), default));
    }

    [Fact]
    public async Task Signup_creates_a_customer_and_never_stores_the_plain_password()
    {
        var result = await _auth.SignupAsync(new SignupCommand("New Person", "New@Test.local", "Secret123!", null), default);

        var saved = Assert.Single(_users.Users);
        Assert.Equal(Roles.Customer, result.User.Role);
        Assert.Equal("new@test.local", saved.Email);
        Assert.NotNull(saved.PasswordHash);
        Assert.DoesNotContain("Secret123!", saved.PasswordHash);
        Assert.Equal(1, _users.SaveCount);
    }

    [Fact]
    public async Task Signup_with_an_existing_email_is_rejected()
    {
        AddUser("customer1", "customer1", FakeUserRepository.CustomerRole);

        await Assert.ThrowsAsync<ConflictException>(
            () => _auth.SignupAsync(new SignupCommand("Copy", "customer1@test.local", "Secret123!", null), default));
    }

    [Fact]
    public async Task GetUser_with_an_unknown_id_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _auth.GetUserAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task Same_password_hashes_differently_for_each_user()
    {
        var first = AddUser("a", "same-password", FakeUserRepository.CustomerRole);
        var second = AddUser("b", "same-password", FakeUserRepository.CustomerRole);

        Assert.NotEqual(first.PasswordHash, second.PasswordHash);
        await Task.CompletedTask;
    }
}
