using ArenaGaming.Api.Services;
using ArenaGaming.Api.Workers;
using DotPulsar;
using DotPulsar.Abstractions;

namespace ArenaGaming.Api.Configuration;

public static class PulsarConfiguration
{
    public static IServiceCollection AddPulsarServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Pulsar client
        var pulsarServiceUrl = configuration.GetConnectionString("Pulsar") ?? "pulsar://localhost:6650";
        
        services.AddSingleton<IPulsarClient>(serviceProvider =>
        {
            return PulsarClient.Builder()
                .ServiceUrl(new Uri(pulsarServiceUrl))
                .Build();
        });

        // Register notification service
        services.AddScoped<IPulsarNotificationService, PulsarNotificationService>();

        // Register background workers
        services.AddHostedService<NotificationWorker>();
        services.AddHostedService<NotificationSimulatorWorker>();

        return services;
    }
} 