using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SwiftDrop.Parcels.Data.Entities;
using SwiftDrop.Parcels.Data.Persistence;
using SwiftDrop.Parcels.Data.Repositories;
using SwiftDrop.Parcels.Data.Storage;

namespace SwiftDrop.Parcels.UnitTests.TestSupport;

/// A real ParcelsDbContext on an in-memory SQLite database, so the tests exercise
/// the actual repository queries and constraints.
internal sealed class ParcelsTestDb : IDisposable
{
    private readonly SqliteConnection connection = new("DataSource=:memory:");

    public ParcelsTestDb()
    {
        connection.Open();
        Context = NewContext();
        Context.Database.EnsureCreated();
        Repository = new ParcelRepository(Context);
    }

    public ParcelsDbContext Context { get; }
    public ParcelRepository Repository { get; }
    public FixedTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 9, 8, 0, 0, TimeSpan.Zero));
    public InMemoryPhotoStorage Photos { get; } = new();

    /// A separate context on the same database, to check what was really saved.
    public ParcelsDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ParcelsDbContext>().UseSqlite(connection).Options);

    public async Task<Parcel> AddParcelAsync(Guid? courierId = null, Guid? customerId = null)
    {
        var parcel = new Parcel(customerId ?? Guid.NewGuid(), $"SD-TEST-{Guid.NewGuid():N}"[..20], Guid.NewGuid(),
            "12 Galle Road, Colombo 03", "Sender Perera", "+94771234567",
            "45 Temple Street, Kandy", "Nimal Silva", "+94779876543",
            2.5m, "Small", 850m, "COD");
        if (courierId is not null) parcel.AssignCourier(courierId.Value, Clock.GetUtcNow().UtcDateTime);
        Context.Parcels.Add(parcel);
        await Context.SaveChangesAsync();
        return parcel;
    }

    public void Dispose()
    {
        Context.Dispose();
        connection.Dispose();
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
}

internal sealed class InMemoryPhotoStorage : IProofPhotoStorage
{
    public Dictionary<string, byte[]> Files { get; } = [];

    public async Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken ct)
    {
        using var copy = new MemoryStream();
        await content.CopyToAsync(copy, ct);
        var key = $"{Guid.NewGuid():N}.{fileExtension}";
        Files[key] = copy.ToArray();
        return key;
    }

    public Stream? OpenRead(string key) => Files.TryGetValue(key, out var bytes) ? new MemoryStream(bytes) : null;

    public void Delete(string key) => Files.Remove(key);
}
