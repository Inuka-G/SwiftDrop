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
        ServiceType serviceType,
        bool isActive = true
    )

    {
        ZoneId = zoneId;
        BaseRate = baseRate;
        AdditionalKgRate = additionalKgRate;
        ServiceType = serviceType;
        IsActive = isActive;
    }

    public Guid ZoneId {get; private set;}

    public decimal BaseRate {get; private set;}

    public decimal AdditionalKgRate {get; private set;}

    public ServiceType ServiceType {get; private set;}

    public bool IsActive {get; private set;}

    public Zone Zone {get; private set;} = default!;

    public void UpdateRates(decimal baseRate, decimal additionalKgRate)
    {
        BaseRate = baseRate;
        AdditionalKgRate = additionalKgRate;
    }

}
