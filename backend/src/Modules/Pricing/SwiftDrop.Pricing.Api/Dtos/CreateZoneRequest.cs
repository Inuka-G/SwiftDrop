using System.ComponentModel.DataAnnotations;

namespace SwiftDrop.Pricing.Api.Dtos;

public record CreateZoneRequest(
    [Required, StringLength(100)] string Name,
    [Range(0, 100000)] decimal BaseFee,
    [Range(0, 100000)] decimal PerKgRate);