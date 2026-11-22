using Microsoft.EntityFrameworkCore;
using SwiftDrop.Pricing.Data.Entities;
using SwiftDrop.Pricing.Contracts;

namespace SwiftDrop.Pricing.Data.Persistence;

internal static class PricingDbSeeder
{
    public static async Task SeedAsync(PricingDbContext db)
    {
        var colomboId = Guid.Parse("AD71E3A6-CCB2-470D-A2FA-7A0693655505");
        var suburbsId = Guid.Parse("EA0BA66E-40A0-4059-B12E-5461E5FC12EA");
        var outstationId = Guid.Parse("A255E58C-45C2-4499-B459-EC06E67FF144");
        var remoteId = Guid.Parse("EE844BE9-E029-4FD8-B749-EE16B1DD3809");
        var testZoneId = Guid.Parse("789EC18E-55CC-4C7E-BF34-713F87449B77");

        if (!await db.Zones.AnyAsync())
        {
            var colombo = new Zone(colomboId, "Colombo", "Colombo delivery area");
            var suburbs = new Zone(suburbsId, "Suburbs", "Suburban delivery areas");
            var outstation = new Zone(outstationId, "Outstation", "Outstation delivery areas");
            var remote = new Zone(remoteId, "Remote", "Remote delivery areas");
            var testZone = new Zone(testZoneId, "A test zone", "Test delivery area");

            db.Zones.AddRange(colombo, suburbs, outstation, remote, testZone);
            await db.SaveChangesAsync();
        }

        var defaultRules = new[]
        {
            // Colombo
            new PricingRule(Guid.Parse("36562FE9-ED15-4209-B8AC-EDBB02FF2C37"), colomboId, 350m, 80m, ServiceType.Standard),
            new PricingRule(Guid.Parse("6BF1972B-83BB-4883-BFE8-A26CC7B4FFF5"), colomboId, 600m, 80m, ServiceType.Express),

            // Suburbs
            new PricingRule(Guid.Parse("AB948E86-DD90-416F-9CD3-3086A5E210A4"), suburbsId, 400m, 100m, ServiceType.Standard),
            new PricingRule(Guid.Parse("804E4FAC-51B1-46C1-B066-746A4ED4F66B"), suburbsId, 650m, 100m, ServiceType.Express),

            // Outstation
            new PricingRule(Guid.Parse("45AA1B5D-8258-4EE1-9CFA-144CF345D7E8"), outstationId, 500m, 120m, ServiceType.Standard),
            new PricingRule(Guid.Parse("C07222ED-664A-4ED2-883D-95CC99101812"), outstationId, 750m, 120m, ServiceType.Express),

            // Remote
            new PricingRule(Guid.Parse("121DE0E2-4716-475D-812F-C4EAC6CB2543"), remoteId, 550m, 120m, ServiceType.Standard),
            new PricingRule(Guid.Parse("75E10648-68A2-4C46-B06B-E267754A09A4"), remoteId, 800m, 120m, ServiceType.Express),

            // Test Zone
            new PricingRule(Guid.Parse("E5A734D8-689B-4D04-8E82-7E1A63F83C10"), testZoneId, 350m, 80m, ServiceType.Standard),
            new PricingRule(Guid.Parse("F7B912C4-4321-4EF5-9A10-BC524E8F0921"), testZoneId, 600m, 80m, ServiceType.Express)
        };

        foreach (var rule in defaultRules)
        {
            var exists = await db.PricingRules.AnyAsync(r => r.ZoneId == rule.ZoneId && r.ServiceType == rule.ServiceType);
            if (!exists)
            {
                db.PricingRules.Add(rule);
            }
        }

        await db.SaveChangesAsync();
    }
}