using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Dispatch.Data.Persistence;
using SwiftDrop.Dispatch.Data.Repositories;
using SwiftDrop.SharedKernel.Persistence;

namespace SwiftDrop.Dispatch.Data;

public static class DispatchDataExtensions
{
    public static IServiceCollection AddDispatchData(this IServiceCollection services, IConfiguration config)
    {
        services.AddModuleDbContext<DispatchDbContext>(config, "Dispatch");
        services.AddScoped<IDispatchRepository, DispatchRepository>();
        return services;
    }

    public static async Task InitializeDispatchDataAsync(this IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DispatchDbContext>().Database.EnsureCreatedAsync();
    }
}
