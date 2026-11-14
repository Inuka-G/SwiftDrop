using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SwiftDrop.Identity.Business.Services;
using SwiftDrop.Identity.Data.Entities;
using SwiftDrop.SharedKernel.Authorization;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Identity.UnitTests;

public class GoogleLoginTests
{
    private readonly FakeUserRepository _users = new();
    private readonly PasswordHasher<User> _hasher = new();
    private readonly AuthService _auth;

    public GoogleLoginTests()
    {
        var jwt = Options.Create(new JwtOptions { Key = new string('k', JwtOptions.MinimumKeyBytes) });
        var codes = new GoogleLoginCodeStore(new MemoryCache(new MemoryCacheOptions()));
        _auth = new AuthService(_users, _hasher, new JwtTokenService(jwt), codes);
    }

    private static GoogleLoginCommand Google(string id = "google-123", string email = "Person@Gmail.com",
        bool verified = true) => new(id, email, "Google Person", verified);

    private async Task<AuthResultDto> SignInAsync(GoogleLoginCommand command) =>
        await _auth.ExchangeGoogleCodeAsync(await _auth.CompleteGoogleLoginAsync(command, default), default);

    [Fact]
    public async Task First_Google_sign_in_creates_a_customer_with_no_password()
    {
        var result = await SignInAsync(Google());

        var saved = Assert.Single(_users.Users);
        Assert.Equal(Roles.Customer, result.User.Role);
        Assert.Equal("person@gmail.com", saved.Email);
        Assert.Equal(User.GoogleProvider, saved.AuthProvider);
        Assert.Equal("google-123", saved.ProviderUserId);
        Assert.Null(saved.PasswordHash);
        Assert.False(string.IsNullOrEmpty(result.AccessToken));
    }

    [Fact]
    public async Task Signing_in_again_reuses_the_same_account()
    {
        var first = await SignInAsync(Google());
        var second = await SignInAsync(Google());

        Assert.Single(_users.Users);
        Assert.Equal(first.User.Id, second.User.Id);
    }

    [Fact]
    public async Task Unverified_Google_email_is_rejected()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _auth.CompleteGoogleLoginAsync(Google(verified: false), default));

        Assert.Empty(_users.Users);
    }

    [Fact]
    public async Task Email_of_an_existing_password_account_is_not_taken_over()
    {
        var local = new User("Local Person", "person@gmail.com", null, null, FakeUserRepository.CustomerRole);
        local.SetPasswordHash(_hasher.HashPassword(local, "Secret123!"));
        _users.Users.Add(local);

        await Assert.ThrowsAsync<ConflictException>(() => _auth.CompleteGoogleLoginAsync(Google(), default));

        Assert.Equal(User.LocalProvider, Assert.Single(_users.Users).AuthProvider);
    }

    [Fact]
    public async Task Deactivated_Google_user_cannot_sign_in()
    {
        await SignInAsync(Google());
        _users.Users[0].Deactivate();

        await Assert.ThrowsAsync<UnauthorizedException>(() => _auth.CompleteGoogleLoginAsync(Google(), default));
    }

    [Fact]
    public async Task One_time_code_works_only_once()
    {
        var code = await _auth.CompleteGoogleLoginAsync(Google(), default);
        await _auth.ExchangeGoogleCodeAsync(code, default);

        await Assert.ThrowsAsync<UnauthorizedException>(() => _auth.ExchangeGoogleCodeAsync(code, default));
    }

    [Fact]
    public async Task Made_up_code_is_rejected()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => _auth.ExchangeGoogleCodeAsync("made-up-code", default));
    }

    [Fact]
    public async Task Google_user_cannot_log_in_with_a_password()
    {
        await SignInAsync(Google());

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _auth.LoginAsync(new LoginCommand("person@gmail.com", "anything"), default));
    }
}
