using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace SwiftDrop.DatabaseMigrator;

public sealed class PostgresImporter(string connectionString)
{
    public async Task<bool> VerifyConnectionAsync(CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1;";
        var res = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(res) == 1;
    }

    public async Task<bool> HasConflictingSeededDataAsync(SqliteExtractor extractor, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var sqliteUsers = extractor.ExtractUsers();
        if (sqliteUsers.Count == 0) return false;

        // Check if usernames exist with different IDs in Postgres
        foreach (var u in sqliteUsers)
        {
            if (string.IsNullOrEmpty(u.Username)) continue;
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT \"Id\" FROM identity.users WHERE \"Username\" = @u;";
            cmd.Parameters.AddWithValue("u", u.Username);
            var destId = await cmd.ExecuteScalarAsync(ct);
            if (destId is Guid g && g != u.Id)
            {
                // Found a seeded user with different ID
                return true;
            }
        }

        // Check if pricing.zones exist with different IDs
        var sqliteZones = extractor.ExtractZones();
        foreach (var z in sqliteZones)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT \"Id\" FROM pricing.zones WHERE \"ZoneName\" = @zn;";
            cmd.Parameters.AddWithValue("zn", z.ZoneName);
            var destId = await cmd.ExecuteScalarAsync(ct);
            if (destId is Guid g && g != z.Id)
            {
                return true;
            }
        }

        // Check if reporting.daily_statistics has shifted dates (e.g. dates not in SQLite)
        var sqliteStats = extractor.ExtractDailyStatistics();
        if (sqliteStats.Count > 0)
        {
            var firstDate = sqliteStats[0].Date;
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT count(*) FROM reporting.daily_statistics WHERE \"Date\" = @d;";
            cmd.Parameters.AddWithValue("d", NpgsqlDbType.Date, firstDate);
            var hasFirstDate = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) > 0;

            await using var cmdCount = conn.CreateCommand();
            cmdCount.CommandText = "SELECT count(*) FROM reporting.daily_statistics;";
            var totalCount = Convert.ToInt32(await cmdCount.ExecuteScalarAsync(ct));

            if (totalCount > 0 && !hasFirstDate)
            {
                // Has rows, but not matching SQLite date range -> seeded
                return true;
            }
        }

        return false;
    }

    public async Task ImportAsync(
        SqliteExtractor extractor,
        bool cleanConflictingSeeded,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            // If requested, clean up transient seeded records in reverse dependency order
            if (cleanConflictingSeeded)
            {
                Console.WriteLine("Cleaning transient seeded demo data in destination tables...");
                await ExecuteNonQueryAsync(conn, tx, "DELETE FROM pricing.pricing_rules;", ct);
                await ExecuteNonQueryAsync(conn, tx, "DELETE FROM pricing.zones;", ct);
                await ExecuteNonQueryAsync(conn, tx, "DELETE FROM reporting.daily_statistics;", ct);
                await ExecuteNonQueryAsync(conn, tx, "DELETE FROM identity.users;", ct);
            }

            // 1. Identity Roles
            var roles = extractor.ExtractRoles();
            Console.WriteLine($"Migrating {roles.Count} identity roles...");
            foreach (var r in roles)
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO identity.roles ("Id", "Name")
                    VALUES (@id, @name)
                    ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name";
                    """;
                cmd.Parameters.AddWithValue("id", r.Id);
                cmd.Parameters.AddWithValue("name", r.Name);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // 2. Identity Users
            var users = extractor.ExtractUsers();
            Console.WriteLine($"Migrating {users.Count} identity users...");
            foreach (var u in users)
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO identity.users (
                        "Id", "FullName", "Email", "Username", "PhoneNumber", "PasswordHash",
                        "AuthProvider", "ProviderUserId", "IsActive", "UpdatedAtUtc", "RoleId", "CreatedAtUtc"
                    )
                    VALUES (
                        @id, @fullName, @email, @username, @phone, @hash,
                        @provider, @providerUserId, @isActive, @updatedAt, @roleId, @createdAt
                    )
                    ON CONFLICT ("Id") DO UPDATE SET
                        "FullName" = EXCLUDED."FullName",
                        "Email" = EXCLUDED."Email",
                        "Username" = EXCLUDED."Username",
                        "PhoneNumber" = EXCLUDED."PhoneNumber",
                        "PasswordHash" = EXCLUDED."PasswordHash",
                        "AuthProvider" = EXCLUDED."AuthProvider",
                        "ProviderUserId" = EXCLUDED."ProviderUserId",
                        "IsActive" = EXCLUDED."IsActive",
                        "UpdatedAtUtc" = EXCLUDED."UpdatedAtUtc",
                        "RoleId" = EXCLUDED."RoleId",
                        "CreatedAtUtc" = EXCLUDED."CreatedAtUtc";
                    """;
                cmd.Parameters.AddWithValue("id", u.Id);
                cmd.Parameters.AddWithValue("fullName", u.FullName);
                cmd.Parameters.AddWithValue("email", u.Email);
                cmd.Parameters.AddWithValue("username", (object?)u.Username ?? DBNull.Value);
                cmd.Parameters.AddWithValue("phone", (object?)u.PhoneNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("hash", (object?)u.PasswordHash ?? DBNull.Value);
                cmd.Parameters.AddWithValue("provider", u.AuthProvider);
                cmd.Parameters.AddWithValue("providerUserId", (object?)u.ProviderUserId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("isActive", u.IsActive);
                cmd.Parameters.AddWithValue("updatedAt", NpgsqlDbType.TimestampTz, u.UpdatedAtUtc);
                cmd.Parameters.AddWithValue("roleId", u.RoleId);
                cmd.Parameters.AddWithValue("createdAt", NpgsqlDbType.TimestampTz, u.CreatedAtUtc);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // 3. Pricing Zones
            var zones = extractor.ExtractZones();
            Console.WriteLine($"Migrating {zones.Count} pricing zones (including 'A test zone')...");
            foreach (var z in zones)
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO pricing.zones ("Id", "ZoneName", "Description", "IsActive", "CreatedAtUtc")
                    VALUES (@id, @name, @desc, @isActive, @createdAt)
                    ON CONFLICT ("Id") DO UPDATE SET
                        "ZoneName" = EXCLUDED."ZoneName",
                        "Description" = EXCLUDED."Description",
                        "IsActive" = EXCLUDED."IsActive",
                        "CreatedAtUtc" = EXCLUDED."CreatedAtUtc";
                    """;
                cmd.Parameters.AddWithValue("id", z.Id);
                cmd.Parameters.AddWithValue("name", z.ZoneName);
                cmd.Parameters.AddWithValue("desc", (object?)z.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("isActive", z.IsActive);
                cmd.Parameters.AddWithValue("createdAt", NpgsqlDbType.TimestampTz, z.CreatedAtUtc);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // 4. Pricing Rules
            var rules = extractor.ExtractPricingRules();
            Console.WriteLine($"Migrating {rules.Count} pricing rules...");
            foreach (var r in rules)
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO pricing.pricing_rules (
                        "Id", "ZoneId", "BaseRate", "AdditionalKgRate", "ServiceType", "IsActive", "CreatedAtUtc"
                    )
                    VALUES (
                        @id, @zoneId, @baseRate, @kgRate, @serviceType, @isActive, @createdAt
                    )
                    ON CONFLICT ("Id") DO UPDATE SET
                        "ZoneId" = EXCLUDED."ZoneId",
                        "BaseRate" = EXCLUDED."BaseRate",
                        "AdditionalKgRate" = EXCLUDED."AdditionalKgRate",
                        "ServiceType" = EXCLUDED."ServiceType",
                        "IsActive" = EXCLUDED."IsActive",
                        "CreatedAtUtc" = EXCLUDED."CreatedAtUtc";
                    """;
                cmd.Parameters.AddWithValue("id", r.Id);
                cmd.Parameters.AddWithValue("zoneId", r.ZoneId);
                cmd.Parameters.AddWithValue("baseRate", r.BaseRate);
                cmd.Parameters.AddWithValue("kgRate", r.AdditionalKgRate);
                cmd.Parameters.AddWithValue("serviceType", r.ServiceType);
                cmd.Parameters.AddWithValue("isActive", r.IsActive);
                cmd.Parameters.AddWithValue("createdAt", NpgsqlDbType.TimestampTz, r.CreatedAtUtc);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // 5. Reporting Daily Statistics
            var stats = extractor.ExtractDailyStatistics();
            Console.WriteLine($"Migrating {stats.Count} reporting daily statistics...");
            foreach (var s in stats)
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO reporting.daily_statistics ("Date", "CreatedCount", "DeliveredCount", "FailedCount", "UpdatedAt")
                    VALUES (@date, @created, @delivered, @failed, @updatedAt)
                    ON CONFLICT ("Date") DO UPDATE SET
                        "CreatedCount" = EXCLUDED."CreatedCount",
                        "DeliveredCount" = EXCLUDED."DeliveredCount",
                        "FailedCount" = EXCLUDED."FailedCount",
                        "UpdatedAt" = EXCLUDED."UpdatedAt";
                    """;
                cmd.Parameters.AddWithValue("date", NpgsqlDbType.Date, s.Date);
                cmd.Parameters.AddWithValue("created", s.CreatedCount);
                cmd.Parameters.AddWithValue("delivered", s.DeliveredCount);
                cmd.Parameters.AddWithValue("failed", s.FailedCount);
                cmd.Parameters.AddWithValue("updatedAt", s.UpdatedAt);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // 6. Reporting Processed Events
            var events = extractor.ExtractReportingProcessedEvents();
            if (events.Count > 0)
            {
                Console.WriteLine($"Migrating {events.Count} reporting processed events...");
                foreach (var ev in events)
                {
                    await using var cmd = conn.CreateCommand();
                    cmd.Transaction = tx;
                    cmd.CommandText = """
                        INSERT INTO reporting.processed_events ("EventId", "ProcessedAt")
                        VALUES (@eventId, @processedAt)
                        ON CONFLICT ("EventId") DO NOTHING;
                        """;
                    cmd.Parameters.AddWithValue("eventId", ev.EventId);
                    cmd.Parameters.AddWithValue("processedAt", ev.ProcessedAt);
                    await cmd.ExecuteNonQueryAsync(ct);
                }
            }

            await tx.CommitAsync(ct);
            Console.WriteLine("Transaction successfully committed to PostgreSQL!");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Transaction rolled back due to error: {ex.Message}");
            Console.ResetColor();
            throw;
        }
    }

    private static async Task ExecuteNonQueryAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string sql, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
