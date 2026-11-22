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

        // Events: outbox rows are published to RabbitMQ in the background
        // (IEventPublisher is registered by AddRabbitMq in the Host)
        services.Configure<OutboxOptions>(config.GetSection(OutboxOptions.SectionName));
        services.AddScoped<OutboxDispatcher>();
        services.AddHostedService<OutboxProcessor>();
        return services;
    }

    // Expose Data initialization through the Business layer
    public static Task InitializeParcelsAsync(this IServiceProvider sp) =>
        sp.InitializeParcelsDataAsync();
}
