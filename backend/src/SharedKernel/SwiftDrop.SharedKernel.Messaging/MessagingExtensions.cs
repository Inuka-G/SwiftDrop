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
        services.AddSingleton<RabbitMqEventPublisher>();
        services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<RabbitMqEventPublisher>());
        services.AddSingleton<IIntegrationEventPublisher>(sp => sp.GetRequiredService<RabbitMqEventPublisher>());
        return services;
    }

    /// Call from a consuming module: one durable queue per module per event.
    public static IServiceCollection AddEventConsumer<TEvent>(this IServiceCollection services, string queueName)
        where TEvent : IntegrationEvent =>
        services.AddEventConsumer<TEvent>(queueName,
            typeof(TEvent).GetCustomAttributes(typeof(RoutingKeyAttribute), inherit: false)
                .Cast<RoutingKeyAttribute>().Single().Key);

    public static IServiceCollection AddEventConsumer<TEvent>(this IServiceCollection services, string queueName,
        string routingKey, string? previousRoutingKey = null) where TEvent : class
    {
        services.AddSingleton<IHostedService>(sp => new EventConsumerService<TEvent>(
            queueName, routingKey, previousRoutingKey,
            sp.GetRequiredService<RabbitMqConnection>(),
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<ILogger<EventConsumerService<TEvent>>>()));
        return services;
    }
}