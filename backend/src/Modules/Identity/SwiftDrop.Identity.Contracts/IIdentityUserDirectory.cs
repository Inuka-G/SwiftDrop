namespace SwiftDrop.Identity.Contracts;

/// <summary>Read-only identity lookups exposed to other modules through public contracts.</summary>
public interface IIdentityUserDirectory
{
    Task<bool> IsActiveCourierAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct);
}
