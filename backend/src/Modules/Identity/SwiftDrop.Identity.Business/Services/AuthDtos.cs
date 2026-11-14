namespace SwiftDrop.Identity.Business.Services;

public record LoginCommand(string Username, string Password);
public record SignupCommand(string FullName, string Email, string Password, string? PhoneNumber);
public record UserDto(Guid Id, string FullName, string Email, string? Username, string Role);
public record AuthResultDto(string AccessToken, DateTime ExpiresAtUtc, UserDto User);
public record AccessToken(string Value, DateTime ExpiresAtUtc);

/// What Google tells us about the person after a successful sign-in.
public record GoogleLoginCommand(string GoogleUserId, string Email, string? FullName, bool EmailVerified);
