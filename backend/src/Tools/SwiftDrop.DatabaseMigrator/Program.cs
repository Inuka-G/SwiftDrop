using System.Text.Json;
using Npgsql;

namespace SwiftDrop.DatabaseMigrator;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("===================================================================");
        Console.WriteLine("   SwiftDrop - SQLite to PostgreSQL One-Time Data Migrator        ");
        Console.WriteLine("===================================================================");
        Console.ResetColor();

        string? sourceDir = null;
        string? connectionString = null;
        var dryRun = false;
        var force = false;
        var validateOnly = false;
        var reportPath = "migration-report.json";

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--source-dir" when i + 1 < args.Length:
                    sourceDir = args[++i];
                    break;
                case "--connection-string" when i + 1 < args.Length:
                    connectionString = args[++i];
                    break;
                case "--report-path" when i + 1 < args.Length:
                    reportPath = args[++i];
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--force":
                    force = true;
                    break;
                case "--validate-only":
                    validateOnly = true;
                    break;
                case "--help" or "-h":
                    PrintHelp();
                    return 0;
            }
        }

        // 1. Resolve source directory
        sourceDir ??= ResolveDefaultSourceDir();
        Console.WriteLine($"Source Directory: {sourceDir}");
        if (!Directory.Exists(sourceDir))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: Source directory does not exist: {sourceDir}");
            Console.ResetColor();
            return 1;
        }

        // 2. Resolve PostgreSQL connection string
        connectionString ??= Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5433;Database=swiftdrop;Username=postgres;Password=postgres;Include Error Detail=true;";

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        Console.WriteLine($"Destination: {builder.Host}:{builder.Port} / DB: {builder.Database} / User: {builder.Username}");
        Console.WriteLine($"Mode: {(validateOnly ? "Validate Only" : dryRun ? "Dry Run" : "Live Migration")}");
        Console.WriteLine($"Force/Replace Seeded: {force}\n");

        var report = new MigrationReportModel
        {
            SourceDirectory = sourceDir,
            DestinationHost = builder.Host ?? "localhost",
            DestinationPort = builder.Port,
            DestinationDatabase = builder.Database ?? "swiftdrop",
            DryRun = dryRun,
            Success = false
        };

        try
        {
            // 3. Backup and SQLite Integrity Checks
            var backupService = new DatabaseBackupService();
            Console.WriteLine("[Step 1/4] Checking SQLite source integrity and creating backups...");
            var (backupOk, backups, backupErrors) = backupService.CreateBackups(sourceDir);
            report.BackupsCreated.AddRange(backups);

            if (!backupOk)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var err in backupErrors) Console.WriteLine($"  Error: {err}");
                Console.ResetColor();
                return 1;
            }

            foreach (var b in backups)
            {
                Console.WriteLine($"  Created backup: {Path.GetFileName(b)}");
            }
            Console.WriteLine("  Source integrity check: OK.\n");

            // 4. Destination Verification
            Console.WriteLine("[Step 2/4] Verifying PostgreSQL connection and schema...");
            var importer = new PostgresImporter(connectionString);
            var extractor = new SqliteExtractor(sourceDir);

            if (!await importer.VerifyConnectionAsync())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: Cannot connect to PostgreSQL destination.");
                Console.ResetColor();
                return 1;
            }
            Console.WriteLine("  PostgreSQL connection: OK.\n");

            // Check for conflicting seeded records in destination
            if (!validateOnly)
            {
                var hasConflicts = await importer.HasConflictingSeededDataAsync(extractor);
                if (hasConflicts && !force)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("WARNING: Conflicting seeded demo data detected in PostgreSQL destination!");
                    Console.WriteLine("  The target tables contain seeded demo records (with different GUIDs/dates) from startup.");
                    Console.WriteLine("  To safely replace conflicting seeded records with authentic SQLite records, run with: --force");
                    Console.ResetColor();
                    return 1;
                }
            }

            // 5. Run Migration
            if (!validateOnly && !dryRun)
            {
                Console.WriteLine("[Step 3/4] Executing migration transaction...");
                await importer.ImportAsync(extractor, cleanConflictingSeeded: force);
                Console.WriteLine("  Migration completed successfully.\n");
            }
            else
            {
                Console.WriteLine("[Step 3/4] Skipping migration write (DryRun / ValidateOnly).\n");
            }

            // 6. Validate Data
            Console.WriteLine("[Step 4/4] Validating source versus destination data integrity...");
            var validator = new MigrationValidator(connectionString, extractor);
            var tableReports = await validator.ValidateAsync();
            report.Tables.AddRange(tableReports);

            PrintValidationTable(tableReports);

            var allPassed = tableReports.All(t => t.CountMatches && t.PrimaryKeysMatch && t.DataIntegrityMatches);
            report.Success = allPassed;

            if (allPassed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nSUCCESS: All source records successfully verified in PostgreSQL destination with 100% data integrity!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nFAILED: Validation discrepancies found between SQLite source and PostgreSQL destination.");
                foreach (var t in tableReports.Where(t => t.Discrepancies.Count > 0))
                {
                    Console.WriteLine($"\nDiscrepancies in {t.Schema}.{t.Table}:");
                    foreach (var d in t.Discrepancies) Console.WriteLine($"  - {d}");
                }
                Console.ResetColor();
            }

            // Write JSON report
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(reportPath, json);
            Console.WriteLine($"\nDetailed JSON migration report saved to: {Path.GetFullPath(reportPath)}");

            return allPassed ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nFATAL ERROR during migration: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
            report.Messages.Add(ex.ToString());
            report.Success = false;
            await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            return 1;
        }
    }

    private static void PrintValidationTable(List<TableValidationReport> reports)
    {
        Console.WriteLine("-------------------------------------------------------------------------------------------------------");
        Console.WriteLine("| Schema       | Table                 | Source | Dest | Counts? | PKs Match? | Integrity? | Status |");
        Console.WriteLine("-------------------------------------------------------------------------------------------------------");
        foreach (var r in reports)
        {
            var status = (r.CountMatches && r.PrimaryKeysMatch && r.DataIntegrityMatches) ? "PASS" : "FAIL";
            Console.WriteLine(
                $"| {r.Schema,-12} | {r.Table,-21} | {r.SourceCount,6} | {r.DestinationCount,4} | " +
                $"{(r.CountMatches ? "YES" : "NO"),-7} | " +
                $"{(r.PrimaryKeysMatch ? "YES" : "NO"),-10} | " +
                $"{(r.DataIntegrityMatches ? "YES" : "NO"),-10} | " +
                $"{status,-6} |");
        }
        Console.WriteLine("-------------------------------------------------------------------------------------------------------");
    }

    private static string ResolveDefaultSourceDir()
    {
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Host", "SwiftDrop.Host"),
            Path.Combine(Directory.GetCurrentDirectory(), "backend", "src", "Host", "SwiftDrop.Host"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Host", "SwiftDrop.Host"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Host", "SwiftDrop.Host"),
            "/Users/inukagunathilake/Myprojects/DotnetProjects/SwiftDrop/backend/src/Host/SwiftDrop.Host"
        };

        foreach (var path in candidates)
        {
            var full = Path.GetFullPath(path);
            if (File.Exists(Path.Combine(full, "identity.db")))
            {
                return full;
            }
        }

        return Path.GetFullPath(candidates[0]);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: dotnet run --project src/Tools/SwiftDrop.DatabaseMigrator [options]");
        Console.WriteLine("\nOptions:");
        Console.WriteLine("  --source-dir <path>          Path to directory containing SQLite database files.");
        Console.WriteLine("  --connection-string <cs>     PostgreSQL connection string.");
        Console.WriteLine("  --dry-run                    Verify source integrity without writing to PostgreSQL.");
        Console.WriteLine("  --force                      Safely replace transient seeded demo data with authentic SQLite records.");
        Console.WriteLine("  --validate-only              Run source-vs-destination validation comparison only.");
        Console.WriteLine("  --report-path <path>         Path for machine-readable JSON migration report (default: migration-report.json).");
        Console.WriteLine("  --help, -h                   Display this help message.");
    }
}
