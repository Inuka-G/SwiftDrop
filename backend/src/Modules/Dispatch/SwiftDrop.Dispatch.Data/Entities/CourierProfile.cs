namespace SwiftDrop.Dispatch.Data.Entities;

internal sealed class CourierProfile
{
    public Guid CourierId { get; set; }
    public Guid? ZoneId { get; set; }
    public bool IsAvailable { get; set; }
    public string? VehicleType { get; set; }
    public string? LicensePlate { get; set; }
    public int Version { get; set; }
}
