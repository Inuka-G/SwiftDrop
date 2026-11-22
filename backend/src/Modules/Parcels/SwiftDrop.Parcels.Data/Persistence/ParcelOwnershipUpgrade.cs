using Microsoft.EntityFrameworkCore;
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
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT name FROM pragma_table_info('parcels')";
        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                existingColumns.Add(reader.GetString(0));
        }

        var copyColumns = db.Model.FindEntityType(typeof(Parcel))!.GetProperties()
            .Where(property => property.Name != nameof(Parcel.MerchantId))
            .Select(property => $"\"{property.GetColumnName()}\"")
            .ToList();
        var copyExpressions = db.Model.FindEntityType(typeof(Parcel))!.GetProperties()
            .Where(property => property.Name != nameof(Parcel.MerchantId))
            .Select(property =>
            {
                var columnName = property.GetColumnName();
                return existingColumns.Contains(columnName)
                    ? $"\"{columnName}\""
                    : "NULL";
            });
        var columnList = string.Join(", ", copyColumns);
        var expressionList = string.Join(", ", copyExpressions);

        var createReplacement = """CREATE TABLE "parcels_upgrade" """ + create[createPrefix.Length..];
        await db.Database.ExecuteSqlRawAsync(createReplacement);
        // The identifiers come exclusively from the application's EF model.
        var copy = $"""INSERT INTO "parcels_upgrade" ({columnList}) SELECT {expressionList} FROM "parcels" """;
        await db.Database.ExecuteSqlRawAsync(copy);
        await db.Database.ExecuteSqlRawAsync("""
            DROP TABLE "parcels";
            ALTER TABLE "parcels_upgrade" RENAME TO "parcels";
            """);
        await UpgradeSqliteStatusHistoryAsync(db);
        foreach (var statement in statements.Where(sql => sql.StartsWith("CREATE ", StringComparison.Ordinal)
                     && sql.Contains("""INDEX """, StringComparison.Ordinal)
                     && sql.Contains(""" ON "parcels" """, StringComparison.Ordinal)))
            await db.Database.ExecuteSqlRawAsync(statement);

        var violations = await db.Database.SqlQuery<int>(
            $"""SELECT COUNT(*) AS "Value" FROM pragma_foreign_key_check""").SingleAsync();
        if (violations != 0)
            throw new InvalidOperationException("Parcel ownership upgrade encountered foreign key violations.");
    }

    private static async Task UpgradeSqliteStatusHistoryAsync(ParcelsDbContext db)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT name FROM pragma_table_info('status_history')";
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                columns.Add(reader.GetString(0));
        }

        if (columns.Contains("ChangedByUserId") && !columns.Contains("ChangedByActorId"))
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "status_history" RENAME COLUMN "ChangedByUserId" TO "ChangedByActorId" """);
        else if (!columns.Contains("ChangedByActorId"))
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "status_history" ADD COLUMN "ChangedByActorId" TEXT NOT NULL DEFAULT '' """);

        if (!columns.Contains("ChangedByActorType"))
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "status_history" ADD COLUMN "ChangedByActorType" TEXT NOT NULL DEFAULT 'User' """);
    }
}
