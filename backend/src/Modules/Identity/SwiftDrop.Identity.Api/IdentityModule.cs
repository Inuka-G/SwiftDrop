using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SwiftDrop.Identity.Business;
using SwiftDrop.SharedKernel.Authorization;

namespace SwiftDrop.Identity.Api;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddControllers().AddApplicationPart(typeof(IdentityModule).Assembly);
        services.AddUserManagementApi();
        services.AddIdentityBusiness(config);
        services.Configure<GoogleAuthOptions>(config.GetSection(GoogleAuthOptions.SectionName));
        return services;
    }

    public static Task InitializeIdentityModuleAsync(this WebApplication app) =>
        app.Services.InitializeIdentityAsync(seedDemoUsers: app.Environment.IsDevelopment());
}
