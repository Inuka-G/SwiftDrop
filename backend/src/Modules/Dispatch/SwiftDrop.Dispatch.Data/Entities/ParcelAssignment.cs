namespace SwiftDrop.Dispatch.Data.Entities;

internal sealed class ParcelAssignment
{
    public Guid ParcelId { get; set; }
    public string TrackingNumber { get; set; } = "";
    public Guid ZoneId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string State { get; set; } = AssignmentStates.Unassigned;
    public Guid? CourierId { get; set; }
    public Guid? AssignedBy { get; set; }
    public DateTimeOffset? AssignedAt { get; set; }
    public int Version { get; set; }
}

internal static class AssignmentStates
{
    public const string Unassigned = "UNASSIGNED";
    public const string Assigned = "ASSIGNED";
    public const string Completed = "COMPLETED";
}
