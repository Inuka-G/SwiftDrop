using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Parcels.Business.Services;
using SwiftDrop.Parcels.Business.Validation;
using SwiftDrop.Parcels.Contracts.Dtos;
using SwiftDrop.Parcels.Data;

namespace SwiftDrop.Parcels.Business;

public static class ParcelsBusinessExtensions
{
    public static IServiceCollection AddParcelsBusiness(this IServiceCollection services, IConfiguration config)
    {
        services.AddParcelsData(config);
        services.AddScoped<IParcelService, ParcelService>();
        services.AddScoped<IValidator<CreateParcelRequest>, CreateParcelValidator>();
        return services;
    }

    // Expose Data initialization through the Business layer
    public static Task InitializeParcelsAsync(this IServiceProvider sp) =>
        sp.InitializeParcelsDataAsync();
}