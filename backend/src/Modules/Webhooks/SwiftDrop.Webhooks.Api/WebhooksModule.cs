using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Webhooks.Business;

namespace SwiftDrop.Webhooks.Api;

public static class WebhooksModule
{
    public static IServiceCollection AddWebhooksModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddControllers().AddApplicationPart(typeof(WebhooksModule).Assembly);
        services.AddWebhooksBusiness();
        return services;
    }
}
