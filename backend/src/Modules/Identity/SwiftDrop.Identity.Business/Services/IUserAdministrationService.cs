namespace SwiftDrop.Identity.Business.Services;

public interface IUserAdministrationService
{
    Task<UserAdministrationResult<UserAdministrationPage>> ListAsync(UserAdministrationQuery query, CancellationToken ct);
    Task<UserAdministrationResult<UserAdministrationDto>> SetStatusAsync(Guid actorId, Guid userId, bool isActive, CancellationToken ct);
    Task<UserAdministrationResult<UserAdministrationDto>> ChangeRoleAsync(Guid actorId, Guid userId, string role, CancellationToken ct);
}
