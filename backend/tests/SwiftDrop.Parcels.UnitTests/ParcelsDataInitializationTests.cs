using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Parcels.Data;
using SwiftDrop.Parcels.Data.Entities;
using SwiftDrop.Parcels.Data.Persistence;

namespace SwiftDrop.Parcels.UnitTests;

/// In Docker all modules share one database. These tests make sure Parcels still
/// creates its tables when another module created its own tables first.
public sealed class ParcelsDataInitializationTests : IDisposable
{
    private readonly SqliteConnection connection = new("DataSource=:memory:");
    private readonly ServiceProvider services;

    public ParcelsDataInitializationTests()
    {
        connection.Open();
        services = new ServiceCollection()
            .AddDbContext<ParcelsDbContext>(o => o.UseSqlite(connection))
            .BuildServiceProvider();
    }

    [Fact]
    public async Task Creates_parcels_tables_when_another_module_already_has_tables()
    {
        Execute("CREATE TABLE users (Id TEXT PRIMARY KEY)");   // e.g. Identity started first

        await services.InitializeParcelsDataAsync();

        TableNames().Should().Contain(["parcels", "status_history", "delivery_proofs", "failure_reports", "outbox_messages"]);
    }

    [Fact]
    public async Task Running_twice_keeps_existing_data()
    {
        await services.InitializeParcelsDataAsync();
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ParcelsDbContext>();
            db.Parcels.Add(new Data.Entities.Parcel(Guid.NewGuid(), "SD-INIT-0001", Guid.NewGuid(),
                "12 Galle Road, Colombo 03", "Sender Perera", "+94771234567",
                "45 Temple Street, Kandy", "Nimal Silva", "+94779876543", 1m, "Small", 500m, "Card"));
            await db.SaveChangesAsync();
        }

        await services.InitializeParcelsDataAsync();   // e.g. the app restarts

        using var check = services.CreateScope();
        (await check.ServiceProvider.GetRequiredService<ParcelsDbContext>().Parcels.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Upgrades_existing_customer_parcels_and_preserves_history()
    {
        Execute("PRAGMA foreign_keys = ON");
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ParcelsDbContext>();
        var originalOwner = Guid.NewGuid();
        var parcelId = Guid.NewGuid();
        var legacySchema = db.Database.GenerateCreateScript()
            .Replace("    \"MerchantId\" TEXT NULL,", "")
            .Replace("\"CustomerId\" TEXT NULL", "\"CustomerId\" TEXT NOT NULL")
            .Replace("""
                ,
                    CONSTRAINT "CK_parcels_single_owner" CHECK (("CustomerId" IS NULL AND "MerchantId" IS NOT NULL) OR ("CustomerId" IS NOT NULL AND "MerchantId" IS NULL))
                """, "")
            .Replace("""CREATE INDEX "IX_parcels_MerchantId" ON "parcels" ("MerchantId");""", "")
            .Replace("\"ChangedByActorId\"", "\"ChangedByUserId\"")
            .Replace("""    "ChangedByActorType" TEXT NOT NULL DEFAULT 'User',""", "");
        Execute(legacySchema);
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO parcels ("Id", "CreatedAtUtc", "UpdatedAtUtc", "CustomerId", "TrackingNumber",
                "ZoneId", "PickupAddress", "PickupContactName", "PickupContactPhone",
                "DropoffAddress", "RecipientName", "RecipientPhone", "WeightKg", "SizeCategory",
                "EstimatedFee", "PaymentMethod", "PaymentStatus", "CurrentStatus", "Version")
            VALUES ({parcelId}, {DateTime.UtcNow}, {DateTime.UtcNow}, {originalOwner}, 'SD-LEGACY',
                {Guid.NewGuid()}, 'Pickup', 'Sender', '+94771234567', 'Dropoff', 'Recipient', '+94779876543',
                1, 'Small', 500, 'COD', 'Pending', 'Created', 1)
            """);
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO status_history ("Id", "ParcelId", "ToStatus", "ChangedByUserId", "ChangedByRole", "CreatedAtUtc")
            VALUES ({Guid.NewGuid()}, {parcelId}, 'Created', {originalOwner}, 'Customer', {DateTime.UtcNow})
            """);

        await services.InitializeParcelsDataAsync();
        await services.InitializeParcelsDataAsync();

        var parcel = await db.Parcels.SingleAsync();
        parcel.CustomerId.Should().Be(originalOwner);
        parcel.MerchantId.Should().BeNull();
        var history = await db.StatusHistory.SingleAsync();
        history.ParcelId.Should().Be(parcelId);
        history.ChangedByActorId.Should().Be(originalOwner);
        history.ChangedByActorType.Should().Be(ParcelStatusActorTypes.User);
        Func<Task> noOwner = () => db.Database.ExecuteSqlRawAsync("""UPDATE parcels SET "CustomerId" = NULL""");
        Func<Task> twoOwners = () => db.Database.ExecuteSqlAsync($"""UPDATE parcels SET "MerchantId" = {Guid.NewGuid()}""");
        await noOwner.Should().ThrowAsync<SqliteException>().WithMessage("*CK_parcels_single_owner*");
        await twoOwners.Should().ThrowAsync<SqliteException>().WithMessage("*CK_parcels_single_owner*");
    }

    private void Execute(string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private List<string> TableNames()
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";
        using var reader = command.ExecuteReader();
        var names = new List<string>();
        while (reader.Read()) names.Add(reader.GetString(0));
        return names;
    }

    public void Dispose()
    {
        services.Dispose();
        connection.Dispose();
    }
}
