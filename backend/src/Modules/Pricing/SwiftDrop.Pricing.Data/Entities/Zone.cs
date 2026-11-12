using SwiftDrop.SharedKernel.Abstractions;

namespace SwiftDrop.Pricing.Data.Entities;

public class Zone : BaseEntity
{
    private Zone() { }   // required by EF Core

    public Zone(string zoneName, string? description)
    {
        ZoneName = zoneName;
        Description = description;
    }

    public string ZoneName {get; private set;} = default!;
    public string? Description {get; private set;}
    public bool IsActive {get; private set;} = true;

    public ICollection<PricingRule> PricingRules {get; private set;}
    = new List<PricingRule>();
}