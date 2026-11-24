using Microsoft.EntityFrameworkCore;
using SwiftDrop.Parcels.Data.Entities;
using SwiftDrop.Parcels.Data.Persistence;

namespace SwiftDrop.Parcels.Data.Repositories;

public interface IOutboxRepository
{
    /// All unsent messages, oldest first. Transient broker outages must not discard parcel events.
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

internal sealed class OutboxRepository(ParcelsDbContext db) : IOutboxRepository
{
    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct) =>
        await db.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
