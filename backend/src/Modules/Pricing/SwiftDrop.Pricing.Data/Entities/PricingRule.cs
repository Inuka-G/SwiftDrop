using SwiftDrop.SharedKernel.Abstractions;
using SwiftDrop.Pricing.Contracts;

namespace SwiftDrop.Pricing.Data.Entities;

public class PricingRule : BaseEntity
{
    private PricingRule() {} //REquired by EF Core

    public PricingRule(
        Guid zoneId,
        decimal baseRate,
        decimal additionalKgRate,
        ServiceType serviceType
    )

    {
        ZoneId = zoneId;
        BaseRate = baseRate;
        AdditionalKgRate = additionalKgRate;
        ServiceType = serviceType;
    }

    public Guid ZoneId {get; private set;}

    public decimal BaseRate {get; private set;}

    public decimal AdditionalKgRate {get; private set;}

    public ServiceType ServiceType {get; private set;}

    public bool IsActive {get; private set;} = true;

    public Zone Zone {get; private set;} = default!;

}