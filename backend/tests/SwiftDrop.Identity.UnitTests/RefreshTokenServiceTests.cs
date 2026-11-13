using Microsoft.Extensions.Options;
using SwiftDrop.Identity.Business.Services;
using SwiftDrop.Identity.Data.Entities;
using SwiftDrop.SharedKernel.Authorization;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Identity.UnitTests;

public class RefreshTokenServiceTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeRefreshTokenRepository _tokens = new();
    private readonly RefreshTokenService _service;
    private readonly User _user;

    public RefreshTokenServiceTests()
    {
        var jwt = Options.Create(new JwtOptions { Key = new string('k', JwtOptions.MinimumKeyBytes), RefreshTokenDays = 7 });
        _service = new RefreshTokenService(_tokens, _users, new JwtTokenService(jwt), jwt);
        _user = new User("Test User", "user@test.local", "user", null, FakeUserRepository.CustomerRole);
        _users.Users.Add(_user);
    }

    [Fact]
    public async Task Stored_token_is_a_hash_not_the_token_itself()
    {
        var issued = await _service.IssueAsync(_user.Id, persistent: true, default);

        var stored = Assert.Single(_tokens.Tokens);
        Assert.NotEqual(issued.Token, stored.TokenHash);
        Assert.DoesNotContain(issued.Token, stored.TokenHash);
    }

    [Fact]
    public async Task Refresh_returns_a_new_access_token_and_a_new_refresh_token()
    {
        var issued = await _service.IssueAsync(_user.Id, persistent: true, default);

        var result = await _service.RefreshAsync(issued.Token, default);

        Assert.False(string.IsNullOrEmpty(result.Auth.AccessToken));
        Assert.Equal(_user.Id, result.Auth.User.Id);
        Assert.NotEqual(issued.Token, result.RefreshToken.Token);
    }

    [Fact]
    public async Task Remember_me_choice_is_kept_after_refresh()
    {
        var issued = await _service.IssueAsync(_user.Id, persistent: false, default);

        var result = await _service.RefreshAsync(issued.Token, default);

        Assert.False(result.RefreshToken.IsPersistent);
    }

    [Fact]
    public async Task Each_refresh_token_works_only_once()
    {
        var issued = await _service.IssueAsync(_user.Id, persistent: true, default);
        await _service.RefreshAsync(issued.Token, default);

        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.RefreshAsync(issued.Token, default));
    }

    [Fact]
    public async Task Reusing_an_old_token_logs_out_every_session_of_that_user()
    {
        var stolen = await _service.IssueAsync(_user.Id, persistent: true, default);
        var current = (await _service.RefreshAsync(stolen.Token, default)).RefreshToken;

        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.RefreshAsync(stolen.Token, default));

        // The token the real user holds no longer works either.
        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.RefreshAsync(current.Token, default));
    }

    [Fact]
    public async Task Revoked_token_cannot_refresh()
    {
        var issued = await _service.IssueAsync(_user.Id, persistent: true, default);
        await _service.RevokeAsync(issued.Token, default);

        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.RefreshAsync(issued.Token, default));
    }

    [Fact]
    public async Task Expired_token_cannot_refresh()
    {
        _tokens.Tokens.Add(new RefreshToken(_user.Id, RefreshTokenService.Hash("old-token"), DateTime.UtcNow.AddMinutes(-1), true));

        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.RefreshAsync("old-token", default));
    }

    [Fact]
    public async Task Deactivated_user_cannot_refresh()
    {
        var issued = await _service.IssueAsync(_user.Id, persistent: true, default);
        _user.Deactivate();

        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.RefreshAsync(issued.Token, default));
    }

    [Theory]
    [InlineData("")]
    [InlineData("made-up-token")]
    public async Task Unknown_token_cannot_refresh(string token)
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.RefreshAsync(token, default));
    }
}
