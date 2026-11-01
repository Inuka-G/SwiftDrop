using SwiftDrop.SharedKernel.Abstractions;

namespace SwiftDrop.Pricing.Data.Entities;

public class Zone : BaseEntity
{
    private Zone() { }   // required by EF Core

    public Zone(string name, decimal baseFee, decimal perKgRate)
    {
        Name = name;
        BaseFee = baseFee;
        PerKgRate = perKgRate;
    }

    public string Name { get; private set; } = default!;
    public decimal BaseFee { get; private set; }
    public decimal PerKgRate { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void Update(string name, decimal baseFee, decimal perKgRate, bool isActive)
    {
        Name = name; BaseFee = baseFee; PerKgRate = perKgRate; IsActive = isActive;
    }
}