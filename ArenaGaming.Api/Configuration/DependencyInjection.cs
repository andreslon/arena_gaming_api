using System;
using ArenaGaming.Core.Application.Interfaces;
using ArenaGaming.Core.Application.Services;
using ArenaGaming.Infrastructure.AI;
using ArenaGaming.Infrastructure.Caching;
using ArenaGaming.Infrastructure.Messaging;
using ArenaGaming.Infrastructure.Persistence;
using DotPulsar;
using DotPulsar.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace ArenaGaming.Api.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Redis (our only data store now!)
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable("ConnectionStrings_Redis") ?? "localhost"));

        // Pulsar Event Publisher with configuration (client configured in PulsarConfiguration.cs)
        services.AddScoped<IEventPublisher>(sp =>
        {
            var pulsarClient = sp.GetRequiredService<IPulsarClient>();
            var tenant = Environment.GetEnvironmentVariable("Pulsar_Tenant") ?? "public";
            var @namespace = Environment.GetEnvironmentVariable("Pulsar_Namespace") ?? "default";
            return new PulsarEventPublisher(pulsarClient, tenant, @namespace);
        });

        // Add Redis-based Repositories (no more PostgreSQL!)
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IMoveRepository, MoveRepository>();

        // Add Services
        services.AddHttpClient<IGeminiService, GeminiService>();
        services.AddScoped<ICacheService, RedisCacheService>();

        // Add application services
        services.AddScoped<GameService>();
        services.AddScoped<SessionService>();

        return services;
    }
} 