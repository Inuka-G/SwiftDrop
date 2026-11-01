using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SwiftDrop.Pricing.Business;

namespace SwiftDrop.Pricing.Api;

public static class PricingModule
{
    public static IServiceCollection AddPricingModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddControllers().AddApplicationPart(typeof(PricingModule).Assembly);
        services.AddPricingBusiness(config);
        return services;
    }

    public static Task InitializePricingModuleAsync(this WebApplication app) =>
        app.Services.InitializePricingAsync(seed: app.Environment.IsDevelopment());
}