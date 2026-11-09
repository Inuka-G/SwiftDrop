using System.ComponentModel.DataAnnotations;

namespace SwiftDrop.Pricing.Api.Dtos;

public record UpdatePricingRuleRequest(
    [Range(0, double.MaxValue)]
    decimal BaseRate,

    [Range(0, double.MaxValue)]
    decimal AdditionalKgRate
);