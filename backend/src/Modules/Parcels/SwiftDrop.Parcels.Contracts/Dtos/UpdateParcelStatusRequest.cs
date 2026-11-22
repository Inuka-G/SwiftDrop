namespace SwiftDrop.Parcels.Contracts.Dtos;

public sealed record UpdateParcelStatusRequest(string Status, string? Note = null);
