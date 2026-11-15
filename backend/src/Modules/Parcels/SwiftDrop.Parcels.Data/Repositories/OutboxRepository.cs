using Microsoft.EntityFrameworkCore;
using SwiftDrop.Parcels.Data.Entities;
using SwiftDrop.Parcels.Data.Persistence;

namespace SwiftDrop.Parcels.Data.Repositories;

public interface IOutboxRepository
{
    /// Unsent messages, oldest first, skipping those that already failed maxAttempts times.
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, int maxAttempts, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

internal sealed class OutboxRepository(ParcelsDbContext db) : IOutboxRepository
{
    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, int maxAttempts, CancellationToken ct) =>
        await db.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null && m.Attempts < maxAttempts)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
