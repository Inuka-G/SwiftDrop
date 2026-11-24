namespace SwiftDrop.Dispatch.Data.Entities;

internal sealed class ParcelAssignment
{
    public Guid ParcelId { get; set; }
    public string TrackingNumber { get; set; } = "";
    public Guid ZoneId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string State { get; set; } = AssignmentStates.Unassigned;
    public string CurrentStatus { get; set; } = "Created";
    public long StatusOccurredAtUtc { get; set; }
    public Guid? CourierId { get; set; }
    public Guid? AssignedBy { get; set; }
    public DateTimeOffset? AssignedAt { get; set; }
    public decimal? WeightKg { get; set; }
    public string? SizeCategory { get; set; }
    public int Version { get; set; }
}

internal static class ParcelDeliveryStatuses
{
    public static bool IsTerminal(string? status) =>
        string.Equals(status, "Delivered", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase);
}

internal static class AssignmentStates
{
    public const string Unassigned = "UNASSIGNED";
    public const string Assigned = "ASSIGNED";
    public const string Completed = "COMPLETED";
}
