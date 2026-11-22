using System.Globalization;
using Microsoft.Data.Sqlite;

namespace SwiftDrop.DatabaseMigrator;

public sealed class SqliteExtractor(string sourceDirectory)
{
    private string GetPath(string fileName) => Path.Combine(sourceDirectory, fileName);

    public List<IdentityRoleRecord> ExtractRoles()
    {
        var list = new List<IdentityRoleRecord>();
        var dbPath = GetPath("identity.db");
        if (!File.Exists(dbPath)) return list;

        using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name FROM roles ORDER BY Id;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new IdentityRoleRecord(reader.GetInt32(0), reader.GetString(1)));
        }
        return list;
    }

    public List<IdentityUserRecord> ExtractUsers()
    {
        var list = new List<IdentityUserRecord>();
        var dbPath = GetPath("identity.db");
        if (!File.Exists(dbPath)) return list;

        using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Id, FullName, Email, Username, PhoneNumber, PasswordHash, 
                   AuthProvider, ProviderUserId, IsActive, UpdatedAtUtc, RoleId, CreatedAtUtc 
            FROM users ORDER BY Username;
            """;
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var id = Guid.Parse(reader.GetString(0));
            var fullName = reader.GetString(1);
            var email = reader.GetString(2);
            var username = reader.IsDBNull(3) ? null : reader.GetString(3);
            var phone = reader.IsDBNull(4) ? null : reader.GetString(4);
            var hash = reader.IsDBNull(5) ? null : reader.GetString(5);
            var provider = reader.GetString(6);
            var providerUserId = reader.IsDBNull(7) ? null : reader.GetString(7);
            var isActive = reader.GetInt32(8) == 1;
            var updatedAt = ParseUtcDateTime(reader.GetString(9));
            var roleId = reader.GetInt32(10);
            var createdAt = ParseUtcDateTime(reader.GetString(11));

            list.Add(new IdentityUserRecord(id, fullName, email, username, phone, hash, provider, providerUserId, isActive, updatedAt, roleId, createdAt));
        }
        return list;
    }

    public List<PricingZoneRecord> ExtractZones()
    {
        var list = new List<PricingZoneRecord>();
        var dbPath = GetPath("pricing.db");
        if (!File.Exists(dbPath)) return list;

        using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, ZoneName, Description, IsActive, CreatedAtUtc FROM zones ORDER BY ZoneName;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var id = Guid.Parse(reader.GetString(0));
            var name = reader.GetString(1);
            var desc = reader.IsDBNull(2) ? null : reader.GetString(2);
            var isActive = reader.GetInt32(3) == 1;
            var createdAt = ParseUtcDateTime(reader.GetString(4));
            list.Add(new PricingZoneRecord(id, name, desc, isActive, createdAt));
        }
        return list;
    }

    public List<PricingRuleRecord> ExtractPricingRules()
    {
        var list = new List<PricingRuleRecord>();
        var dbPath = GetPath("pricing.db");
        if (!File.Exists(dbPath)) return list;

        using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, ZoneId, BaseRate, AdditionalKgRate, ServiceType, IsActive, CreatedAtUtc FROM pricing_rules ORDER BY ServiceType, BaseRate;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var id = Guid.Parse(reader.GetString(0));
            var zoneId = Guid.Parse(reader.GetString(1));
            var baseRate = decimal.Parse(reader.GetString(2), CultureInfo.InvariantCulture);
            var kgRate = decimal.Parse(reader.GetString(3), CultureInfo.InvariantCulture);
            var serviceType = reader.GetString(4);
            var isActive = reader.GetInt32(5) == 1;
            var createdAt = ParseUtcDateTime(reader.GetString(6));
            list.Add(new PricingRuleRecord(id, zoneId, baseRate, kgRate, serviceType, isActive, createdAt));
        }
        return list;
    }

    public List<ReportingDailyStatRecord> ExtractDailyStatistics()
    {
        var list = new List<ReportingDailyStatRecord>();
        var dbPath = GetPath("reporting.db");
        if (!File.Exists(dbPath)) return list;

        using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT ReportDate, CreatedCount, DeliveredCount, FailedCount FROM daily_statistics ORDER BY ReportDate;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var dateStr = reader.GetString(0);
            var date = DateOnly.ParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var created = reader.GetInt32(1);
            var delivered = reader.GetInt32(2);
            var failed = reader.GetInt32(3);
            var epoch = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
            list.Add(new ReportingDailyStatRecord(date, created, delivered, failed, epoch));
        }
        return list;
    }

    public List<ProcessedEventRecord> ExtractReportingProcessedEvents()
    {
        var list = new List<ProcessedEventRecord>();
        var dbPath = GetPath("reporting.db");
        if (!File.Exists(dbPath)) return list;

        using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='processed_events';";
        if (cmd.ExecuteScalar() is null) return list;

        cmd.CommandText = "SELECT EventId, ProcessedAtUtc FROM processed_events;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var eventId = Guid.Parse(reader.GetString(0));
            var timeStr = reader.GetString(1);
            var dt = ParseUtcDateTime(timeStr);
            var epoch = new DateTimeOffset(dt).ToUnixTimeMilliseconds();
            list.Add(new ProcessedEventRecord(eventId, epoch));
        }
        return list;
    }

    public int GetTableCount(string dbName, string tableName)
    {
        var dbPath = GetPath(dbName);
        if (!File.Exists(dbPath)) return 0;

        using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT count(*) FROM sqlite_master WHERE type='table' AND name='{tableName}';";
        if (Convert.ToInt32(cmd.ExecuteScalar()) == 0) return 0;

        cmd.CommandText = $"SELECT count(*) FROM \"{tableName}\";";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static DateTime ParseUtcDateTime(string text)
    {
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
        {
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }
        return DateTime.UtcNow;
    }
}
