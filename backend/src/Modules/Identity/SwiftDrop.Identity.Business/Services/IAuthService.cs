namespace SwiftDrop.Identity.Business.Services;

public interface IAuthService
{
    Task<AuthResultDto> LoginAsync(LoginCommand command, CancellationToken ct);
    Task<AuthResultDto> SignupAsync(SignupCommand command, CancellationToken ct);
    Task<UserDto> GetUserAsync(Guid id, CancellationToken ct);
}
