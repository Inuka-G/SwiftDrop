using Microsoft.Data.Sqlite;

namespace SwiftDrop.DatabaseMigrator;

public sealed class DatabaseBackupService
{
    private static readonly string[] RequiredDatabases =
    [
        "identity.db",
        "pricing.db",
        "reporting.db",
        "parcels.db",
        "dispatch.db",
        "merchants.db"
    ];

    public (bool Success, List<string> Backups, List<string> Errors) CreateBackups(string sourceDirectory)
    {
        var backups = new List<string>();
        var errors = new List<string>();

        if (!Directory.Exists(sourceDirectory))
        {
            errors.Add($"Source directory does not exist: {sourceDirectory}");
            return (false, backups, errors);
        }

        var backupDir = Path.Combine(sourceDirectory, "backups");
        Directory.CreateDirectory(backupDir);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        foreach (var dbName in RequiredDatabases)
        {
            var dbPath = Path.Combine(sourceDirectory, dbName);
            if (!File.Exists(dbPath))
            {
                // Note: file might not exist if not yet touched
                continue;
            }

            // 1. Verify SQLite integrity
            try
            {
                using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA integrity_check;";
                var result = cmd.ExecuteScalar()?.ToString();
                if (result != "ok")
                {
                    errors.Add($"SQLite integrity check failed for {dbName}: {result}");
                    continue;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Cannot open source database {dbName} for integrity check: {ex.Message}");
                continue;
            }

            // 2. Create copy
            try
            {
                var backupPath = Path.Combine(backupDir, $"{dbName}.{timestamp}.bak");
                File.Copy(dbPath, backupPath, overwrite: true);
                backups.Add(backupPath);
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to create backup for {dbName}: {ex.Message}");
            }
        }

        return (errors.Count == 0, backups, errors);
    }
}
