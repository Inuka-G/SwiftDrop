using Microsoft.AspNetCore.Identity;
using SwiftDrop.Identity.Data.Entities;
using SwiftDrop.Identity.Data.Repositories;
using SwiftDrop.SharedKernel.Authorization;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Identity.Business.Services;

internal sealed class AuthService(
    IUserRepository users,
    IPasswordHasher<User> hasher,
    ITokenService tokens) : IAuthService
{
    public async Task<AuthResultDto> LoginAsync(LoginCommand command, CancellationToken ct)
    {
        var user = await users.FindByLoginAsync(Normalize(command.Username), ct);

        // Same error for every failure, so an attacker cannot discover which usernames exist.
        if (user is null || !user.IsActive || user.PasswordHash is null ||
            hasher.VerifyHashedPassword(user, user.PasswordHash, command.Password) == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedException("Incorrect username or password.");
        }

        return ToResult(user);
    }

    public async Task<AuthResultDto> SignupAsync(SignupCommand command, CancellationToken ct)
    {
        var email = Normalize(command.Email);
        if (await users.FindByLoginAsync(email, ct) is not null)
            throw new ConflictException("An account with this email already exists.");

        // Public signup always creates a customer. The role never comes from the request,
        // otherwise anyone could sign up as an admin.
        var role = await users.GetRoleAsync(Roles.Customer, ct)
            ?? throw new InvalidOperationException("The CUSTOMER role is missing from the roles table.");

        var user = new User(command.FullName.Trim(), email, username: null, command.PhoneNumber, role);
        user.SetPasswordHash(hasher.HashPassword(user, command.Password));

        await users.AddAsync(user, ct);
        await users.SaveChangesAsync(ct);
        return ToResult(user);
    }

    public async Task<UserDto> GetUserAsync(Guid id, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(id, ct) ?? throw new NotFoundException("User not found.");
        return ToDto(user);
    }

    private AuthResultDto ToResult(User user)
    {
        var token = tokens.CreateToken(user);
        return new AuthResultDto(token.Value, token.ExpiresAtUtc, ToDto(user));
    }

    private static UserDto ToDto(User u) => new(u.Id, u.FullName, u.Email, u.Username, u.Role.Name);

    private static string Normalize(string login) => login.Trim().ToLowerInvariant();
}
