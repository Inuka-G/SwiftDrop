using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Merchants.Business.Dtos;
using SwiftDrop.Merchants.Business.Services;
using SwiftDrop.Merchants.Business.Validators;
using SwiftDrop.Merchants.Contracts;
using SwiftDrop.Merchants.Data;

namespace SwiftDrop.Merchants.Business;

public static class MerchantsBusinessExtensions
{
    public static IServiceCollection AddMerchantsBusiness(this IServiceCollection services, IConfiguration config)
    {
        services.AddMerchantsData(config);
        services.AddScoped<IValidator<RegisterMerchantCommand>, RegisterMerchantValidator>();
        services.AddScoped<IMerchantEventPublisher, UnconfiguredMerchantEventPublisher>();
        services.AddScoped<MerchantService>();
        services.AddScoped<IMerchantService>(sp => sp.GetRequiredService<MerchantService>());
        services.AddScoped<IMerchantAccess>(sp => sp.GetRequiredService<MerchantService>());
        return services;
    }
}

