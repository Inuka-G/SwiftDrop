using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Pricing.Business.Services;
using SwiftDrop.Pricing.Contracts;
using SwiftDrop.Pricing.Data;

namespace SwiftDrop.Pricing.Business;

public static class PricingBusinessExtensions
{
    public static IServiceCollection AddPricingBusiness(this IServiceCollection services, IConfiguration config)
    {
        services.AddPricingData(config);                 // Business is allowed to talk to Data
        services.AddScoped<IZoneService, ZoneService>();
        services.AddScoped<IPricingService, PricingService>();
        return services;
    }

    public static Task InitializePricingAsync(this IServiceProvider sp, bool seed) =>
        sp.InitializePricingDataAsync(seed);
}