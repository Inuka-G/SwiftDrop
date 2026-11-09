using System.ComponentModel.DataAnnotations;

namespace SwiftDrop.Pricing.Api.Dtos;

public record CreateZoneRequest(
    [Required, StringLength(100)] string ZoneName,
    [StringLength(250)] string? Description
    );