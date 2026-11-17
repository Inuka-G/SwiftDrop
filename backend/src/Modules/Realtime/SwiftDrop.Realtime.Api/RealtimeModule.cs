
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Realtime.Api.Hubs;
using SwiftDrop.Realtime.Api.Services;
using SwiftDrop.Realtime.Business.Services;

namespace SwiftDrop.Realtime.Api;

public static class RealtimeModule
{
    public static IServiceCollection AddRealtimeModule(
        this IServiceCollection services)
    {
        services.AddSignalR();

        services.AddScoped<ITrackingNotifier, TrackingNotifier>();

        return services;
    }

    public static WebApplication MapRealtimeEndpoints(
        this WebApplication app)
    {
        app.MapHub<TrackingHub>("/hubs/tracking");

        return app;
    }
}
