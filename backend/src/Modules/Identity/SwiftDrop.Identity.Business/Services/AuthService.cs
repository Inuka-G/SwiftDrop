using Microsoft.AspNetCore.Identity;
using SwiftDrop.Identity.Data.Entities;
using SwiftDrop.Identity.Data.Repositories;
using SwiftDrop.SharedKernel.Authorization;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Identity.Business.Services;

internal sealed class AuthService(
    IUserRepository users,
    IPasswordHasher<User> hasher,
    ITokenService tokens,
    IGoogleLoginCodeStore googleCodes) : IAuthService
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

    public async Task<string> CompleteGoogleLoginAsync(GoogleLoginCommand command, CancellationToken ct)
    {
        var user = await users.FindByProviderAsync(User.GoogleProvider, command.GoogleUserId, ct);

        if (user is null)
        {
            if (!command.EmailVerified)
                throw new UnauthorizedException("Your Google email address is not verified.");

            var email = Normalize(command.Email);

            // We never attach Google to an existing password account automatically: the owner
            // of that account did not agree to it, so they must log in with their password.
            if (await users.FindByLoginAsync(email, ct) is not null)
                throw new ConflictException("An account with this email already exists. Log in with your password instead.");

            // Like public signup, Google sign-in always creates a customer.
            var role = await users.GetRoleAsync(Roles.Customer, ct)
                ?? throw new InvalidOperationException("The CUSTOMER role is missing from the roles table.");

            var fullName = string.IsNullOrWhiteSpace(command.FullName) ? email : command.FullName.Trim();
            user = User.CreateGoogleUser(fullName, email, command.GoogleUserId, role);
            await users.AddAsync(user, ct);
            await users.SaveChangesAsync(ct);
        }

        if (!user.IsActive)
            throw new UnauthorizedException("This account has been deactivated.");

        return googleCodes.Create(user.Id);
    }

    public async Task<AuthResultDto> ExchangeGoogleCodeAsync(string code, CancellationToken ct)
    {
        var userId = googleCodes.Redeem(code);
        var user = userId is null ? null : await users.GetByIdAsync(userId.Value, ct);

        if (user is null || !user.IsActive)
            throw new UnauthorizedException("This Google sign-in has expired. Please sign in with Google again.");

        return ToResult(user);
    }

    private AuthResultDto ToResult(User user)
    {
        var token = tokens.CreateToken(user);
        return new AuthResultDto(token.Value, token.ExpiresAtUtc, ToDto(user));
    }

    private static UserDto ToDto(User u) => new(u.Id, u.FullName, u.Email, u.Username, u.Role.Name);

    private static string Normalize(string login) => login.Trim().ToLowerInvariant();
}
