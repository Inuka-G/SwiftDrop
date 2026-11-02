namespace SwiftDrop.Identity.Business.Services;

public record LoginCommand(string Username, string Password);
public record SignupCommand(string FullName, string Email, string Password, string? PhoneNumber);
public record UserDto(Guid Id, string FullName, string Email, string? Username, string Role);
public record AuthResultDto(string AccessToken, DateTime ExpiresAtUtc, UserDto User);
public record AccessToken(string Value, DateTime ExpiresAtUtc);
