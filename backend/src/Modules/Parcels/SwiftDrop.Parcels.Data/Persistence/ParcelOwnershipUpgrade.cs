using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using SwiftDrop.Parcels.Data.Entities;

namespace SwiftDrop.Parcels.Data.Persistence;

internal static class ParcelOwnershipUpgrade
{
    public static Task ApplyAsync(ParcelsDbContext db) =>
        db.Database.IsNpgsql() ? UpgradePostgresAsync(db) : UpgradeSqliteAsync(db);

    private static async Task UpgradePostgresAsync(ParcelsDbContext db)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        // Serialize startup upgrades across Host instances sharing this schema.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(286540611, 1)");
        var upgraded = await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*)::int AS "Value" FROM information_schema.columns
            WHERE table_schema = 'parcels' AND table_name = 'parcels' AND column_name = 'MerchantId'
            """).SingleAsync();

        if (upgraded == 0)
        {
            await db.Database.ExecuteSqlRawAsync("""
                ALTER TABLE parcels.parcels ADD COLUMN "MerchantId" uuid,
                    ALTER COLUMN "CustomerId" DROP NOT NULL,
                    ADD CONSTRAINT "CK_parcels_single_owner" CHECK (
                        ("CustomerId" IS NULL AND "MerchantId" IS NOT NULL)
                        OR ("CustomerId" IS NOT NULL AND "MerchantId" IS NULL));
                CREATE INDEX "IX_parcels_MerchantId" ON parcels.parcels ("MerchantId");
                ALTER TABLE parcels.status_history RENAME COLUMN "ChangedByUserId" TO "ChangedByActorId";
                ALTER TABLE parcels.status_history ADD COLUMN "ChangedByActorType" character varying(16)
                    NOT NULL DEFAULT 'User';
                """);
        }

        await transaction.CommitAsync();
    }

    private static async Task UpgradeSqliteAsync(ParcelsDbContext db)
    {
        await db.Database.OpenConnectionAsync();
        try
        {
            using var pragma = db.Database.GetDbConnection().CreateCommand();
            pragma.CommandText = "PRAGMA foreign_keys";
            var foreignKeysEnabled = Convert.ToInt32(await pragma.ExecuteScalarAsync()) == 1;

            // SQLite requires a table rebuild to change nullability and add the check
            // constraint. Disable cascading actions before rebuilding the parent table.
            await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF");
            try
            {
                await using var transaction = await db.Database.BeginTransactionAsync();
                var upgraded = await db.Database.SqlQuery<int>($"""
                    SELECT COUNT(*) AS "Value" FROM pragma_table_info('parcels') WHERE name = 'MerchantId'
                    """).SingleAsync();
                if (upgraded == 0)
                    await RebuildSqliteTableAsync(db);

                await UpgradeSqliteStatusHistoryAsync(db);
                await EnsureSqliteOutboxTableAsync(db);

                var violations = await db.Database.SqlQuery<int>(
                    $"""SELECT COUNT(*) AS "Value" FROM pragma_foreign_key_check""").SingleAsync();
                if (violations != 0)
                    throw new InvalidOperationException("Parcel ownership upgrade encountered foreign key violations.");

                await transaction.CommitAsync();
            }
            finally
            {
                if (foreignKeysEnabled)
                    await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON");
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static async Task RebuildSqliteTableAsync(ParcelsDbContext db)
    {
        const string createPrefix = """CREATE TABLE "parcels" """;
        var statements = db.Database.GenerateCreateScript()
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var create = statements.Single(sql => sql.StartsWith(createPrefix, StringComparison.Ordinal));
        var parcelType = db.Model.FindEntityType(typeof(Parcel))!;
        var table = StoreObjectIdentifier.Table(parcelType.GetTableName()!, parcelType.GetSchema());
        var modelColumns = parcelType.GetProperties()
            .Select(property => (Property: property, ColumnName: property.GetColumnName(table)))
            .Where(column => column.ColumnName is not null)
            .Select(column => (column.Property, ColumnName: column.ColumnName!))
            .ToList();

        var existingColumns = await ReadSqliteParcelColumnsAsync(db);
        var missingRequiredColumns = modelColumns
            .Where(column => !column.Property.IsNullable && !existingColumns.Contains(column.ColumnName))
            .Select(column => column.ColumnName)
            .ToArray();
        if (missingRequiredColumns.Length > 0)
        {
            throw new InvalidOperationException(
                $"Cannot upgrade the parcels table because required columns are missing: {string.Join(", ", missingRequiredColumns)}.");
        }

        var copyColumns = modelColumns
            .Where(column => existingColumns.Contains(column.ColumnName))
            .Select(column => QuoteSqliteIdentifier(column.ColumnName))
            .ToArray();
        if (copyColumns.Length == 0)
            throw new InvalidOperationException("Cannot upgrade the parcels table because it has no columns in common with the current model.");

        var columnList = string.Join(", ", copyColumns);

        var createReplacement = """CREATE TABLE "parcels_upgrade" """ + create[createPrefix.Length..];
        await db.Database.ExecuteSqlRawAsync(createReplacement);
        // New nullable model columns are omitted from the insert and remain NULL.
        var copy = $"""INSERT INTO {QuoteSqliteIdentifier("parcels_upgrade")} ({columnList}) SELECT {columnList} FROM {QuoteSqliteIdentifier("parcels")}""";
        await db.Database.ExecuteSqlRawAsync(copy);
        await db.Database.ExecuteSqlRawAsync("""
            DROP TABLE "parcels";
            ALTER TABLE "parcels_upgrade" RENAME TO "parcels";
            """);
        foreach (var statement in statements.Where(sql => sql.StartsWith("CREATE ", StringComparison.Ordinal)
                     && sql.Contains("""INDEX """, StringComparison.Ordinal)
                     && sql.Contains(""" ON "parcels" """, StringComparison.Ordinal)))
            await db.Database.ExecuteSqlRawAsync(statement);

    }

    private static async Task UpgradeSqliteStatusHistoryAsync(ParcelsDbContext db)
    {
        var tableExists = await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS "Value" FROM sqlite_master
            WHERE type = 'table' AND name = 'status_history'
            """).SingleAsync() > 0;

        var statements = db.Database.GenerateCreateScript()
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (!tableExists)
        {
            const string createPrefix = """CREATE TABLE "status_history" """;
            var create = statements.SingleOrDefault(sql => sql.StartsWith(createPrefix, StringComparison.Ordinal));
            if (create is null)
                throw new InvalidOperationException("The EF model does not define the status_history table.");

            await db.Database.ExecuteSqlRawAsync(create);
            foreach (var statement in statements.Where(sql => sql.StartsWith("CREATE ", StringComparison.Ordinal)
                         && sql.Contains("""INDEX """, StringComparison.Ordinal)
                         && sql.Contains(""" ON "status_history" """, StringComparison.Ordinal)))
                await db.Database.ExecuteSqlRawAsync(statement);

            return;
        }

        var columns = await ReadSqliteTableColumnsAsync(db, "status_history");
        if (columns.Contains("ChangedByUserId") && !columns.Contains("ChangedByActorId"))
            await db.Database.ExecuteSqlRawAsync("""
                ALTER TABLE "status_history" RENAME COLUMN "ChangedByUserId" TO "ChangedByActorId";
                """);
        else if (!columns.Contains("ChangedByActorId"))
            throw new InvalidOperationException(
                "Cannot upgrade status_history because neither ChangedByUserId nor ChangedByActorId exists.");

        if (!columns.Contains("ChangedByActorType"))
            await db.Database.ExecuteSqlRawAsync("""
                ALTER TABLE "status_history" ADD COLUMN "ChangedByActorType" TEXT NOT NULL DEFAULT 'User';
                """);
    }

    private static async Task EnsureSqliteOutboxTableAsync(ParcelsDbContext db)
    {
        var tableExists = await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS "Value" FROM sqlite_master
            WHERE type = 'table' AND name = 'outbox_messages'
            """).SingleAsync() > 0;

        var statements = db.Database.GenerateCreateScript()
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!tableExists)
        {
            const string createPrefix = """CREATE TABLE "outbox_messages" """;
            var create = statements.SingleOrDefault(sql => sql.StartsWith(createPrefix, StringComparison.Ordinal));
            if (create is null)
                throw new InvalidOperationException("The EF model does not define the outbox_messages table.");

            await db.Database.ExecuteSqlRawAsync(create);
        }

        var outboxType = db.Model.FindEntityType(typeof(OutboxMessage))
            ?? throw new InvalidOperationException("The EF model does not define the OutboxMessage entity.");
        foreach (var index in outboxType.GetIndexes())
        {
            var indexName = index.GetDatabaseName()
                ?? throw new InvalidOperationException("The EF model contains an unnamed outbox_messages index.");
            var indexExists = await db.Database.SqlQuery<int>($"""
                SELECT COUNT(*) AS "Value" FROM sqlite_master
                WHERE type = 'index' AND tbl_name = 'outbox_messages' AND name = {indexName}
                """).SingleAsync() > 0;
            if (indexExists)
                continue;

            var quotedIndexName = QuoteSqliteIdentifier(indexName);
            var createIndex = statements.SingleOrDefault(sql => sql.StartsWith("CREATE ", StringComparison.Ordinal)
                && sql.Contains("INDEX", StringComparison.Ordinal)
                && sql.Contains(quotedIndexName, StringComparison.Ordinal)
                && sql.Contains(""" ON "outbox_messages" """, StringComparison.Ordinal));
            if (createIndex is null)
                throw new InvalidOperationException($"The EF model does not define SQL for outbox_messages index '{indexName}'.");

            await db.Database.ExecuteSqlRawAsync(createIndex);
        }
    }

    private static Task<HashSet<string>> ReadSqliteParcelColumnsAsync(ParcelsDbContext db) =>
        ReadSqliteTableColumnsAsync(db, "parcels");

    private static async Task<HashSet<string>> ReadSqliteTableColumnsAsync(ParcelsDbContext db, string tableName)
    {
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = tableName switch
        {
            "parcels" => "PRAGMA table_info('parcels')",
            "status_history" => "PRAGMA table_info('status_history')",
            _ => throw new ArgumentOutOfRangeException(nameof(tableName), tableName, "Unexpected Parcels table name."),
        };
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();

        await using var reader = await command.ExecuteReaderAsync();
        var nameOrdinal = reader.GetOrdinal("name");
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync())
            columns.Add(reader.GetString(nameOrdinal));

        return columns;
    }

    private static string QuoteSqliteIdentifier(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
