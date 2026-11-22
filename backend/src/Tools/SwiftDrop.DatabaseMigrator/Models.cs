namespace SwiftDrop.DatabaseMigrator;

public sealed record IdentityRoleRecord(int Id, string Name);

public sealed record IdentityUserRecord(
    Guid Id,
    string FullName,
    string Email,
    string? Username,
    string? PhoneNumber,
    string? PasswordHash,
    string AuthProvider,
    string? ProviderUserId,
    bool IsActive,
    DateTime UpdatedAtUtc,
    int RoleId,
    DateTime CreatedAtUtc);

public sealed record PricingZoneRecord(
    Guid Id,
    string ZoneName,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record PricingRuleRecord(
    Guid Id,
    Guid ZoneId,
    decimal BaseRate,
    decimal AdditionalKgRate,
    string ServiceType,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record ReportingDailyStatRecord(
    DateOnly Date,
    int CreatedCount,
    int DeliveredCount,
    int FailedCount,
    long UpdatedAt);

public sealed record ProcessedEventRecord(Guid EventId, long ProcessedAt);

public sealed class TableValidationReport
{
    public required string Schema { get; set; }
    public required string Table { get; set; }
    public required int SourceCount { get; set; }
    public required int DestinationCount { get; set; }
    public required bool CountMatches { get; set; }
    public required bool PrimaryKeysMatch { get; set; }
    public required bool DataIntegrityMatches { get; set; }
    public List<string> Discrepancies { get; set; } = [];
}

public sealed class MigrationReportModel
{
    public DateTime ExecutedAtUtc { get; set; } = DateTime.UtcNow;
    public required string SourceDirectory { get; set; }
    public required string DestinationHost { get; set; }
    public required int DestinationPort { get; set; }
    public required string DestinationDatabase { get; set; }
    public required bool Success { get; set; }
    public required bool DryRun { get; set; }
    public List<string> BackupsCreated { get; set; } = [];
    public List<TableValidationReport> Tables { get; set; } = [];
    public List<string> Messages { get; set; } = [];
}
