using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SwiftDrop.Pricing.Data.Entities;

namespace SwiftDrop.Pricing.Data.Persistence;

internal sealed class PricingRuleConfiguration: IEntityTypeConfiguration<PricingRule>
{
    public void Configure(EntityTypeBuilder<PricingRule> b)
    {
        b.ToTable("pricing_rules");
        b.HasKey(p => p.Id);
        b.Property(p => p.ZoneId).IsRequired();
        b.Property(p => p.BaseRate).HasPrecision(10,2).IsRequired();
        b.Property(p => p.AdditionalKgRate).HasPrecision(10,2).IsRequired();
        b.Property(p => p.ServiceType).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(p => p.IsActive).IsRequired();
        

        //One zone can have multiple pricing rules
        b.HasOne(p => p.Zone).WithMany(z => z.PricingRules).HasForeignKey(p => p.ZoneId).OnDelete(DeleteBehavior.Restrict);
    }
}