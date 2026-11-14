using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.AIAssistant.Business.Services;

namespace SwiftDrop.AIAssistant.Business;

public static class AiAssistantBusinessExtensions
{
    public static IServiceCollection AddAiAssistantBusiness(this IServiceCollection services)
    {
        services.AddScoped<IAiAssistantService, AiAssistantService>();
        return services;
    }
}

