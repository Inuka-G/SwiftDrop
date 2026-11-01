using Microsoft.EntityFrameworkCore;
using SwiftDrop.Pricing.Data.Entities;

namespace SwiftDrop.Pricing.Data.Persistence;

internal static class PricingDbSeeder
{
    public static async Task SeedAsync(PricingDbContext db)
    {
        if (await db.Zones.AnyAsync()) return;

        db.Zones.AddRange(
            new Zone("Colombo", 300m, 50m),
            new Zone("Kandy", 450m, 70m),
            new Zone("Galle", 500m, 75m));
        await db.SaveChangesAsync();
    }
}