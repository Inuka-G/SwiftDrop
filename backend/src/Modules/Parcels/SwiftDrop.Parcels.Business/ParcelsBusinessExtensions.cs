using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SwiftDrop.Parcels.Business.Outbox;
using SwiftDrop.Parcels.Business.Services;
using SwiftDrop.Parcels.Business.Validation;
using SwiftDrop.Parcels.Contracts;
using SwiftDrop.Parcels.Contracts.Dtos;
using SwiftDrop.Parcels.Data;
using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.Parcels.Business;

public static class ParcelsBusinessExtensions
{
    public static IServiceCollection AddParcelsBusiness(this IServiceCollection services, IConfiguration config)
    {
        services.AddParcelsData(config);
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IParcelService, ParcelService>();
        services.AddScoped<IValidator<CreateParcelRequest>, CreateParcelValidator>();

        // Parcel lifecycle (courier status updates, proof, failure reports)
        services.AddScoped<IParcelLifecycleService, ParcelLifecycleService>();
        services.AddScoped<IParcelAssignment, ParcelAssignmentService>();
        services.AddScoped<IValidator<UpdateDeliveryStatusRequest>, UpdateDeliveryStatusValidator>();
        services.AddScoped<IValidator<ReportFailureRequest>, ReportFailureValidator>();
        services.AddScoped<IValidator<ConfirmDeliveryRequest>, ConfirmDeliveryValidator>();

        // Status events: outbox rows are published to the broker in the background
        services.Configure<OutboxOptions>(config.GetSection(OutboxOptions.SectionName));
        services.AddScoped<OutboxDispatcher>();
        services.AddHostedService<OutboxProcessor>();
        services.AddIntegrationEventPublisherFallback();
        return services;
    }

    // Expose Data initialization through the Business layer
    public static Task InitializeParcelsAsync(this IServiceProvider sp) =>
        sp.InitializeParcelsDataAsync();
}
