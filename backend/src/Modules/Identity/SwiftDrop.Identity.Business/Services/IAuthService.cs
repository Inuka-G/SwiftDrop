namespace SwiftDrop.Identity.Business.Services;

public interface IAuthService
{
    Task<AuthResultDto> LoginAsync(LoginCommand command, CancellationToken ct);
    Task<AuthResultDto> SignupAsync(SignupCommand command, CancellationToken ct);
    Task<UserDto> GetUserAsync(Guid id, CancellationToken ct);

    /// Finds or creates the customer for a Google sign-in and returns a one-time code for them.
    Task<string> CompleteGoogleLoginAsync(GoogleLoginCommand command, CancellationToken ct);

    /// Swaps a one-time code from CompleteGoogleLoginAsync for a normal SwiftDrop login result.
    Task<AuthResultDto> ExchangeGoogleCodeAsync(string code, CancellationToken ct);
}
