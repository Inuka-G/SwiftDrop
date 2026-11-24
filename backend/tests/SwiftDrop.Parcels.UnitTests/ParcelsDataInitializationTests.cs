using System.Text.RegularExpressions;
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
    public async Task Creates_missing_outbox_table_for_existing_parcels_and_preserves_parcels()
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ParcelsDbContext>();
        var schemaStatements = db.Database.GenerateCreateScript()
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(statement => !statement.StartsWith("""CREATE TABLE "outbox_messages" """, StringComparison.Ordinal)
                && !(statement.StartsWith("CREATE ", StringComparison.Ordinal)
                    && statement.Contains("INDEX", StringComparison.Ordinal)
                    && statement.Contains(""" ON "outbox_messages" """, StringComparison.Ordinal)));
        Execute(string.Join(";", schemaStatements));

        var originalOwner = Guid.NewGuid();
        db.Parcels.Add(new Parcel(originalOwner, "SD-NO-OUTBOX", Guid.NewGuid(),
            "Pickup", "Sender", "+94771234567", "Dropoff", "Recipient", "+94779876543",
            1m, "Small", 500m, "Card"));
        await db.SaveChangesAsync();
        TableNames().Should().NotContain("outbox_messages");

        await services.InitializeParcelsDataAsync();
        await services.InitializeParcelsDataAsync();

        var parcel = await db.Parcels.AsNoTracking().SingleAsync();
        parcel.TrackingNumber.Should().Be("SD-NO-OUTBOX");
        parcel.CustomerId.Should().Be(originalOwner);
        TableNames().Should().Contain("outbox_messages");
        OutboxColumnNames().Should().Contain(
            ["Id", "RoutingKey", "Payload", "ProcessedAtUtc", "Attempts", "LastError", "CreatedAtUtc"]);
        (await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS "Value" FROM sqlite_master
            WHERE type = 'index' AND tbl_name = 'outbox_messages' AND name LIKE 'IX_%'
            """).SingleAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Repeated_initialization_preserves_existing_outbox_table_and_messages()
    {
        await services.InitializeParcelsDataAsync();
        var messageId = Guid.NewGuid();
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ParcelsDbContext>();
            db.OutboxMessages.Add(new OutboxMessage(messageId, "parcels.status-changed", "{}", DateTime.UtcNow));
            await db.SaveChangesAsync();
        }

        await services.InitializeParcelsDataAsync();
        await services.InitializeParcelsDataAsync();

        using var check = services.CreateScope();
        var checkDb = check.ServiceProvider.GetRequiredService<ParcelsDbContext>();
        var message = await checkDb.OutboxMessages.AsNoTracking().SingleAsync();
        message.Id.Should().Be(messageId);
        message.RoutingKey.Should().Be("parcels.status-changed");
        message.Payload.Should().Be("{}");
        TableNames().Should().Contain("outbox_messages");
    }

    [Fact]
    public async Task Creates_missing_outbox_index_without_changing_existing_table_data()
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ParcelsDbContext>();
        var schemaStatements = db.Database.GenerateCreateScript()
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(statement => !(statement.StartsWith("CREATE ", StringComparison.Ordinal)
                && statement.Contains("INDEX", StringComparison.Ordinal)
                && statement.Contains(""" ON "outbox_messages" """, StringComparison.Ordinal)));
        Execute(string.Join(";", schemaStatements));

        var messageId = Guid.NewGuid();
        db.OutboxMessages.Add(new OutboxMessage(messageId, "parcels.status-changed", "{}", DateTime.UtcNow));
        await db.SaveChangesAsync();
        (await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS "Value" FROM sqlite_master
            WHERE type = 'index' AND tbl_name = 'outbox_messages' AND name LIKE 'IX_%'
            """).SingleAsync()).Should().Be(0);

        await services.InitializeParcelsDataAsync();
        await services.InitializeParcelsDataAsync();

        var existingMessage = await db.OutboxMessages.AsNoTracking().SingleAsync();
        existingMessage.Id.Should().Be(messageId);
        existingMessage.Payload.Should().Be("{}");
        (await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS "Value" FROM sqlite_master
            WHERE type = 'index' AND tbl_name = 'outbox_messages' AND name LIKE 'IX_%'
            """).SingleAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Upgrades_existing_customer_parcels_and_preserves_history()
    {
        Execute("PRAGMA foreign_keys = ON");
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ParcelsDbContext>();
        var originalOwner = Guid.NewGuid();
        var parcelId = Guid.NewGuid();
        var legacyColumnsMissingFromParcels = new[] { "MerchantId", "CourierId", "AssignedAtUtc" };
        var legacySchema = string.Join(";", db.Database.GenerateCreateScript()
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(statement => !(statement.StartsWith("CREATE ", StringComparison.Ordinal)
                && statement.Contains("INDEX", StringComparison.Ordinal)
                && statement.Contains(" ON \"parcels\" ", StringComparison.Ordinal)
                && legacyColumnsMissingFromParcels.Any(column =>
                    statement.Contains($"\"{column}\"", StringComparison.Ordinal)))))
            .Replace("    \"MerchantId\" TEXT NULL,", "")
            .Replace("    \"CourierId\" TEXT NULL,", "")
            .Replace("    \"AssignedAtUtc\" TEXT NULL,", "")
            .Replace("\"CustomerId\" TEXT NULL", "\"CustomerId\" TEXT NOT NULL")
            .Replace("\"ChangedByActorId\"", "\"ChangedByUserId\"")
            .Replace("""    "ChangedByActorType" TEXT NOT NULL DEFAULT 'User',""", "");
        legacySchema = Regex.Replace(legacySchema,
            @",\s*CONSTRAINT ""CK_parcels_single_owner"" CHECK \(\(""CustomerId"" IS NULL AND ""MerchantId"" IS NOT NULL\) OR \(""CustomerId"" IS NOT NULL AND ""MerchantId"" IS NULL\)\)",
            string.Empty, RegexOptions.CultureInvariant);
        var legacyStatements = legacySchema.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var legacyParcelTable = legacyStatements.Single(statement => statement.StartsWith("CREATE TABLE \"parcels\" ", StringComparison.Ordinal));
        var legacyParcelIndexes = legacyStatements.Where(statement => statement.StartsWith("CREATE ", StringComparison.Ordinal)
            && statement.Contains("INDEX", StringComparison.Ordinal)
            && statement.Contains(" ON \"parcels\" ", StringComparison.Ordinal));
        foreach (var column in legacyColumnsMissingFromParcels)
        {
            legacyParcelTable.Should().NotContain($"\"{column}\"");
            legacyParcelIndexes.Should().NotContain(statement => statement.Contains($"\"{column}\"", StringComparison.Ordinal));
        }
        legacyParcelTable.Should().NotContain("CK_parcels_single_owner");
        Execute(legacySchema);
        ParcelColumnNames().Should().NotContain("MerchantId");
        ParcelColumnNames().Should().NotContain("CourierId");
        ParcelColumnNames().Should().NotContain("AssignedAtUtc");
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
        parcel.CourierId.Should().BeNull();
        parcel.AssignedAtUtc.Should().BeNull();
        var history = await db.StatusHistory.SingleAsync();
        history.ParcelId.Should().Be(parcelId);
        history.ChangedByActorId.Should().Be(originalOwner);
        history.ChangedByActorType.Should().Be(ParcelStatusActorTypes.User);
        Func<Task> noOwner = () => db.Database.ExecuteSqlRawAsync("""UPDATE parcels SET "CustomerId" = NULL""");
        Func<Task> twoOwners = () => db.Database.ExecuteSqlAsync($"""UPDATE parcels SET "MerchantId" = {Guid.NewGuid()}""");
        await noOwner.Should().ThrowAsync<SqliteException>().WithMessage("*CK_parcels_single_owner*");
        await twoOwners.Should().ThrowAsync<SqliteException>().WithMessage("*CK_parcels_single_owner*");
    }

    [Fact]
    public async Task Creates_missing_history_table_for_existing_parcels_and_preserves_parcels()
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ParcelsDbContext>();
        var schemaStatements = db.Database.GenerateCreateScript()
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(statement => !statement.StartsWith("""CREATE TABLE "status_history" """, StringComparison.Ordinal)
                && !(statement.StartsWith("CREATE ", StringComparison.Ordinal)
                    && statement.Contains("INDEX", StringComparison.Ordinal)
                    && statement.Contains(""" ON "status_history" """, StringComparison.Ordinal)));
        Execute(string.Join(";", schemaStatements));

        var originalOwner = Guid.NewGuid();
        db.Parcels.Add(new Parcel(originalOwner, "SD-NO-HISTORY", Guid.NewGuid(),
            "Pickup", "Sender", "+94771234567", "Dropoff", "Recipient", "+94779876543",
            1m, "Small", 500m, "Card"));
        await db.SaveChangesAsync();
        TableNames().Should().NotContain("status_history");

        await services.InitializeParcelsDataAsync();
        await services.InitializeParcelsDataAsync();

        var parcel = await db.Parcels.AsNoTracking().SingleAsync();
        parcel.TrackingNumber.Should().Be("SD-NO-HISTORY");
        parcel.CustomerId.Should().Be(originalOwner);
        (await db.StatusHistory.CountAsync()).Should().Be(0);
        (await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS "Value" FROM pragma_foreign_key_list('status_history') WHERE "table" = 'parcels'
            """).SingleAsync()).Should().Be(1);
        (await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS "Value" FROM sqlite_master
            WHERE type = 'index' AND tbl_name = 'status_history' AND name LIKE 'IX_%'
            """).SingleAsync()).Should().Be(1);
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

    private List<string> ParcelColumnNames()
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('parcels')";
        using var reader = command.ExecuteReader();
        var nameOrdinal = reader.GetOrdinal("name");
        var names = new List<string>();
        while (reader.Read()) names.Add(reader.GetString(nameOrdinal));
        return names;
    }

    private List<string> OutboxColumnNames()
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('outbox_messages')";
        using var reader = command.ExecuteReader();
        var nameOrdinal = reader.GetOrdinal("name");
        var names = new List<string>();
        while (reader.Read()) names.Add(reader.GetString(nameOrdinal));
        return names;
    }

    public void Dispose()
    {
        services.Dispose();
        connection.Dispose();
    }
}
