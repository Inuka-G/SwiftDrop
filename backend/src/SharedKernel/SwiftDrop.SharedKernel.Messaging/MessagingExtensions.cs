using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SwiftDrop.SharedKernel.Messaging;

public static class MessagingExtensions
{
    /// Call once in the Host.
    public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<RabbitMqOptions>(config.GetSection("RabbitMq"));
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        return services;
    }

    /// Call from a consuming module: one durable queue per module per event.
    public static IServiceCollection AddEventConsumer<TEvent>(this IServiceCollection services, string queueName)
        where TEvent : IntegrationEvent
    {
        services.AddSingleton<IHostedService>(sp => new EventConsumerService<TEvent>(
            queueName,
            sp.GetRequiredService<RabbitMqConnection>(),
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<ILogger<EventConsumerService<TEvent>>>()));
        return services;
    }
}