using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.Realtime.Api.Hubs;
using SwiftDrop.Realtime.Api.Services;
using SwiftDrop.Realtime.Business.Services;
using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.Realtime.Api;

public static class RealtimeModule
{
    public static IServiceCollection AddRealtimeModule(
        this IServiceCollection services)
    {
        services.AddSignalR();

        services.AddScoped<ITrackingNotifier, TrackingNotifier>();

        services.AddScoped<
            IIntegrationEventHandler<ParcelStatusChanged>,
            ParcelStatusChangedHandler>();

        services.AddEventConsumer<ParcelStatusChanged>("q.realtime");

        return services;
    }

    public static WebApplication MapRealtimeEndpoints(
        this WebApplication app)
    {
        app.MapHub<TrackingHub>("/hubs/tracking");

        return app;
    }
}