using System.ComponentModel.DataAnnotations;

namespace SwiftDrop.Pricing.Api.Dtos;

public sealed record CreatePricingRuleRequest(
    [Required, Range(1, 2)] int? ServiceType,
    [Required, Range(typeof(decimal), "0", "99999999.99")] decimal? BaseRate,
    [Required, Range(typeof(decimal), "0", "99999999.99")] decimal? AdditionalKgRate,
    [Required] bool? IsActive);
