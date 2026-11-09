using Microsoft.EntityFrameworkCore;
using SwiftDrop.Pricing.Data.Entities;
using SwiftDrop.Pricing.Contracts;

namespace SwiftDrop.Pricing.Data.Persistence;

internal static class PricingDbSeeder
{
    public static async Task SeedAsync(PricingDbContext db)
    {
        if (await db.Zones.AnyAsync()) return;

        var Colombo = new Zone("Colombo", "Colombo delivery area");
        var Suburbs = new Zone("Suburbs", "Suburban delivery areas");
        var Outstation = new Zone ("Outstation", "Outstation delivery areas");
        var Remote = new Zone ("Remote", "Remote delivery areas");

        var zones = new[] {Colombo, Suburbs, Outstation, Remote};

        db.Zones.AddRange(zones);
        await db.SaveChangesAsync();

        db.PricingRules.AddRange(
            //colombo
            new PricingRule(Colombo.Id,350m,80m,ServiceType.Standard),
            new PricingRule(Colombo.Id,600m,80m,ServiceType.Express),

            //Suburbs
            new PricingRule(Suburbs.Id,400m,100m,ServiceType.Standard),
            new PricingRule(Suburbs.Id,650m,100m,ServiceType.Express),

            //Outstation
            new PricingRule(Outstation.Id,500m,120m,ServiceType.Standard),
            new PricingRule(Outstation.Id,750m,120m,ServiceType.Express),

            //Remote
            new PricingRule(Remote.Id,550m,120m,ServiceType.Standard),
            new PricingRule(Remote.Id,800m,120m,ServiceType.Express)
        );

        await db.SaveChangesAsync();
    }
}