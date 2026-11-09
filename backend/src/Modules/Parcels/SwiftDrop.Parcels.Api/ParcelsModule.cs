using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Parcels.Business;

namespace SwiftDrop.Parcels.Api;

public static class ParcelsModule
{
    public static IServiceCollection AddParcelsModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddControllers().AddApplicationPart(typeof(ParcelsModule).Assembly);
        services.AddParcelsBusiness(config);
        return services;
    }

    public static Task InitializeParcelsModuleAsync(this WebApplication app) =>
        app.Services.InitializeParcelsAsync();
}