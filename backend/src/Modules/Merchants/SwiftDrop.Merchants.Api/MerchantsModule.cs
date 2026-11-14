using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Merchants.Business;
using SwiftDrop.Merchants.Data;

namespace SwiftDrop.Merchants.Api;

public static class MerchantsModule
{
    public static IServiceCollection AddMerchantsModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddControllers().AddApplicationPart(typeof(MerchantsModule).Assembly);
        services.AddMerchantsBusiness(config);
        return services;
    }

    public static Task InitializeMerchantsModuleAsync(this WebApplication app) =>
        app.Services.InitializeMerchantsDataAsync();
}

