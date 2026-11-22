using System.Data;
using System.Globalization;
using Npgsql;
using NpgsqlTypes;

namespace SwiftDrop.DatabaseMigrator;

public sealed class MigrationValidator(string connectionString, SqliteExtractor extractor)
{
    public async Task<List<TableValidationReport>> ValidateAsync(CancellationToken ct = default)
    {
        var reports = new List<TableValidationReport>();
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        // 1. identity.roles
        reports.Add(await ValidateRolesAsync(conn, ct));

        // 2. identity.users
        reports.Add(await ValidateUsersAsync(conn, ct));

        // 3. pricing.zones
        reports.Add(await ValidateZonesAsync(conn, ct));

        // 4. pricing.pricing_rules
        reports.Add(await ValidatePricingRulesAsync(conn, ct));

        // 5. reporting.daily_statistics
        reports.Add(await ValidateDailyStatisticsAsync(conn, ct));

        // 6. reporting.processed_events
        reports.Add(await ValidateEmptyTableAsync(conn, "reporting", "processed_events", extractor.GetTableCount("reporting.db", "processed_events"), ct));

        // 7. parcels.parcels
        reports.Add(await ValidateEmptyTableAsync(conn, "parcels", "parcels", extractor.GetTableCount("parcels.db", "parcels"), ct));

        // 8. dispatch.parcel_assignments
        reports.Add(await ValidateEmptyTableAsync(conn, "dispatch", "parcel_assignments", extractor.GetTableCount("dispatch.db", "parcel_assignments"), ct));

        // 9. merchants.merchants
        reports.Add(await ValidateEmptyTableAsync(conn, "merchants", "merchants", extractor.GetTableCount("merchants.db", "merchants"), ct));

        return reports;
    }

    private Task<TableValidationReport> ValidateRolesAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        var src = extractor.ExtractRoles();
        var dest = new Dictionary<int, string>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT \"Id\", \"Name\" FROM identity.roles;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) dest[reader.GetInt32(0)] = reader.GetString(1);

        var report = new TableValidationReport
        {
            Schema = "identity",
            Table = "roles",
            SourceCount = src.Count,
            DestinationCount = dest.Count,
            CountMatches = src.Count == dest.Count,
            PrimaryKeysMatch = true,
            DataIntegrityMatches = true
        };

        foreach (var r in src)
        {
            if (!dest.TryGetValue(r.Id, out var name))
            {
                report.PrimaryKeysMatch = false;
                report.Discrepancies.Add($"Missing role ID {r.Id} ({r.Name}) in destination.");
            }
            else if (name != r.Name)
            {
                report.DataIntegrityMatches = false;
                report.Discrepancies.Add($"Role name mismatch for ID {r.Id}: expected '{r.Name}', found '{name}'.");
            }
        }

        return Task.FromResult(report);
    }

    private Task<TableValidationReport> ValidateUsersAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        var src = extractor.ExtractUsers();
        var dest = new Dictionary<Guid, (string Username, string Email, string? Hash, int RoleId, DateTime CreatedAt)>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT \"Id\", \"Username\", \"Email\", \"PasswordHash\", \"RoleId\", \"CreatedAtUtc\" FROM identity.users;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var id = reader.GetGuid(0);
            var username = reader.GetString(1);
            var email = reader.GetString(2);
            var hash = reader.IsDBNull(3) ? null : reader.GetString(3);
            var roleId = reader.GetInt32(4);
            var createdAt = reader.GetDateTime(5);
            dest[id] = (username, email, hash, roleId, createdAt);
        }

        var report = new TableValidationReport
        {
            Schema = "identity",
            Table = "users",
            SourceCount = src.Count,
            DestinationCount = dest.Count,
            CountMatches = src.Count == dest.Count,
            PrimaryKeysMatch = true,
            DataIntegrityMatches = true
        };

        foreach (var u in src)
        {
            if (!dest.TryGetValue(u.Id, out var d))
            {
                report.PrimaryKeysMatch = false;
                report.Discrepancies.Add($"Missing user GUID {u.Id} ({u.Username}) in destination.");
            }
            else
            {
                if (d.Username != u.Username)
                {
                    report.DataIntegrityMatches = false;
                    report.Discrepancies.Add($"Username mismatch for {u.Id}: expected '{u.Username}', got '{d.Username}'.");
                }
                if (d.Email != u.Email)
                {
                    report.DataIntegrityMatches = false;
                    report.Discrepancies.Add($"Email mismatch for {u.Username}: expected '{u.Email}', got '{d.Email}'.");
                }
                if (d.Hash != u.PasswordHash)
                {
                    report.DataIntegrityMatches = false;
                    report.Discrepancies.Add($"Password hash mismatch for {u.Username}.");
                }
                if (d.RoleId != u.RoleId)
                {
                    report.DataIntegrityMatches = false;
                    report.Discrepancies.Add($"Role ID mismatch for {u.Username}: expected {u.RoleId}, got {d.RoleId}.");
                }
            }
        }

        return Task.FromResult(report);
    }

    private Task<TableValidationReport> ValidateZonesAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        var src = extractor.ExtractZones();
        var dest = new Dictionary<Guid, (string Name, string? Desc, bool IsActive)>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT \"Id\", \"ZoneName\", \"Description\", \"IsActive\" FROM pricing.zones;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            dest[reader.GetGuid(0)] = (
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.GetBoolean(3)
            );
        }

        var report = new TableValidationReport
        {
            Schema = "pricing",
            Table = "zones",
            SourceCount = src.Count,
            DestinationCount = dest.Count,
            CountMatches = src.Count == dest.Count,
            PrimaryKeysMatch = true,
            DataIntegrityMatches = true
        };

        foreach (var z in src)
        {
            if (!dest.TryGetValue(z.Id, out var d))
            {
                report.PrimaryKeysMatch = false;
                report.Discrepancies.Add($"Missing zone GUID {z.Id} ('{z.ZoneName}') in destination.");
            }
            else
            {
                if (d.Name != z.ZoneName)
                {
                    report.DataIntegrityMatches = false;
                    report.Discrepancies.Add($"Zone name mismatch for {z.Id}: expected '{z.ZoneName}', got '{d.Name}'.");
                }
                if (d.IsActive != z.IsActive)
                {
                    report.DataIntegrityMatches = false;
                    report.Discrepancies.Add($"Zone active state mismatch for '{z.ZoneName}'.");
                }
            }
        }

        return Task.FromResult(report);
    }

    private Task<TableValidationReport> ValidatePricingRulesAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        var src = extractor.ExtractPricingRules();
        var dest = new Dictionary<Guid, (Guid ZoneId, decimal BaseRate, decimal KgRate, string ServiceType)>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT \"Id\", \"ZoneId\", \"BaseRate\", \"AdditionalKgRate\", \"ServiceType\" FROM pricing.pricing_rules;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            dest[reader.GetGuid(0)] = (
                reader.GetGuid(1),
                reader.GetDecimal(2),
                reader.GetDecimal(3),
                reader.GetString(4)
            );
        }

        var report = new TableValidationReport
        {
            Schema = "pricing",
            Table = "pricing_rules",
            SourceCount = src.Count,
            DestinationCount = dest.Count,
            CountMatches = src.Count == dest.Count,
            PrimaryKeysMatch = true,
            DataIntegrityMatches = true
        };

        foreach (var r in src)
        {
            if (!dest.TryGetValue(r.Id, out var d))
            {
                report.PrimaryKeysMatch = false;
                report.Discrepancies.Add($"Missing pricing rule GUID {r.Id} in destination.");
            }
            else
            {
                if (d.ZoneId != r.ZoneId)
                {
                    report.DataIntegrityMatches = false;
                    report.Discrepancies.Add($"ZoneId FK mismatch for rule {r.Id}: expected {r.ZoneId}, got {d.ZoneId}.");
                }
                if (d.BaseRate != r.BaseRate)
                {
                    report.DataIntegrityMatches = false;
                    report.Discrepancies.Add($"BaseRate mismatch for rule {r.Id}: expected {r.BaseRate}, got {d.BaseRate}.");
                }
                if (d.KgRate != r.AdditionalKgRate)
                {
                    report.DataIntegrityMatches = false;
                    report.Discrepancies.Add($"AdditionalKgRate mismatch for rule {r.Id}: expected {r.AdditionalKgRate}, got {d.KgRate}.");
                }
            }
        }

        return Task.FromResult(report);
    }

    private Task<TableValidationReport> ValidateDailyStatisticsAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        var src = extractor.ExtractDailyStatistics();
        var dest = new Dictionary<DateOnly, (int Created, int Delivered, int Failed)>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT \"Date\", \"CreatedCount\", \"DeliveredCount\", \"FailedCount\" FROM reporting.daily_statistics;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            dest[DateOnly.FromDateTime(reader.GetDateTime(0))] = (
                reader.GetInt32(1),
                reader.GetInt32(2),
                reader.GetInt32(3)
            );
        }

        var report = new TableValidationReport
        {
            Schema = "reporting",
            Table = "daily_statistics",
            SourceCount = src.Count,
            DestinationCount = dest.Count,
            CountMatches = src.Count == dest.Count,
            PrimaryKeysMatch = true,
            DataIntegrityMatches = true
        };

        foreach (var s in src)
        {
            if (!dest.TryGetValue(s.Date, out var d))
            {
                report.PrimaryKeysMatch = false;
                report.Discrepancies.Add($"Missing daily statistics record for date {s.Date:yyyy-MM-dd} in destination.");
            }
            else
            {
                if (d.Created != s.CreatedCount || d.Delivered != s.DeliveredCount || d.Failed != s.FailedCount)
                {
                    report.DataIntegrityMatches = false;
                    report.Discrepancies.Add($"Count mismatch for {s.Date:yyyy-MM-dd}: expected ({s.CreatedCount},{s.DeliveredCount},{s.FailedCount}), got ({d.Created},{d.Delivered},{d.Failed}).");
                }
            }
        }

        return Task.FromResult(report);
    }

    private async Task<TableValidationReport> ValidateEmptyTableAsync(NpgsqlConnection conn, string schema, string table, int srcCount, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT count(*) FROM \"{schema}\".\"{table}\";";
        var destCount = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));

        return new TableValidationReport
        {
            Schema = schema,
            Table = table,
            SourceCount = srcCount,
            DestinationCount = destCount,
            CountMatches = srcCount == destCount,
            PrimaryKeysMatch = true,
            DataIntegrityMatches = true
        };
    }
}
