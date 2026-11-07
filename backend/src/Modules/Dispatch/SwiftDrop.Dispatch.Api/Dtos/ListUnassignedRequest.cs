namespace SwiftDrop.Dispatch.Api.Dtos;

public sealed record ListUnassignedRequest(Guid? Zone, DateTimeOffset? From, DateTimeOffset? To, int Page = 1, int PageSize = 25);
