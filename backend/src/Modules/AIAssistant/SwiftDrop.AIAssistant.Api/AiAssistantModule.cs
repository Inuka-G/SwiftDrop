using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.AIAssistant.Api.Clients;
using SwiftDrop.AIAssistant.Api.Configuration;
using SwiftDrop.AIAssistant.Api.Tools;
using SwiftDrop.AIAssistant.Business;
using SwiftDrop.AIAssistant.Business.Services;
using SwiftDrop.AIAssistant.Business.Tools;

namespace SwiftDrop.AIAssistant.Api;

public static class AiAssistantModule
{
    public static IServiceCollection AddAiAssistantModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.AddAiAssistantBusiness();
        services.AddHttpClient<ILlmClient, OpenAiCompatibleLlmClient>(client =>
            client.Timeout = TimeSpan.FromSeconds(30));

        var internalApiBaseUrl = configuration[$"{AiOptions.SectionName}:InternalApiBaseUrl"]
                                 ?? "http://localhost:5017/api/v1/";

        services.AddHttpClient<IMcpClient, HttpMcpClient>(client =>
        {
            client.BaseAddress = new Uri(new Uri(internalApiBaseUrl), "ai-assistant/mcp");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<TrackParcelTool>(client =>
            client.BaseAddress = new Uri(internalApiBaseUrl));
        services.AddHttpClient<EstimateFeeTool>(client =>
            client.BaseAddress = new Uri(internalApiBaseUrl));
        services.AddScoped<IAssistantTool>(provider => provider.GetRequiredService<TrackParcelTool>());
        services.AddScoped<IAssistantTool>(provider => provider.GetRequiredService<EstimateFeeTool>());

        services.AddControllers().AddApplicationPart(typeof(AiAssistantModule).Assembly);
        return services;
    }
}

