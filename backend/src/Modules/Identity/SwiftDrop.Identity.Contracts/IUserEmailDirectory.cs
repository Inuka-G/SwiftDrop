namespace SwiftDrop.Identity.Contracts;

public interface IUserEmailDirectory
{
    Task<string?> GetActiveEmailAsync(Guid id, CancellationToken ct);
}
