namespace SwiftDrop.Dispatch.Api.Dtos;

public sealed record ListParcelsRequest(string? Status, string? TrackingNumber, int Page = 1, int PageSize = 25);
