using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Notifications.Business;

namespace SwiftDrop.Notifications.Api;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddControllers().AddApplicationPart(typeof(NotificationsModule).Assembly);
        services.AddNotificationsBusiness(config);
        return services;
    }

    public static Task InitializeNotificationsModuleAsync(this WebApplication app) =>
        app.Services.InitializeNotificationsAsync();
}
