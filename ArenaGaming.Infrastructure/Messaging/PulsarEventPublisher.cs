using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ArenaGaming.Core.Application.Interfaces;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;

namespace ArenaGaming.Infrastructure.Messaging;

public class PulsarEventPublisher : IEventPublisher
{
    private readonly IPulsarClient _pulsarClient;
    private readonly string _tenant;
    private readonly string _namespace;

    public PulsarEventPublisher(IPulsarClient pulsarClient, string tenant, string @namespace)
    {
        _pulsarClient = pulsarClient;
        _tenant = tenant;
        _namespace = @namespace;
    }

    public async Task PublishAsync<T>(T @event, string topic, CancellationToken cancellationToken = default)
    {
        var producer = _pulsarClient.NewProducer()
            .Topic($"{_tenant}/{_namespace}/{topic}")
            .Create();

        try
        {
            var message = JsonSerializer.Serialize(@event);
            var messageId = await producer.Send(Encoding.UTF8.GetBytes(message), cancellationToken);
        }
        finally
        {
            await producer.DisposeAsync();
        }
    }

    Task IEventPublisher.PublishAsync<T>(T @event, string topic, CancellationToken cancellationToken)
    {
        return PublishAsync(@event, topic, cancellationToken);
    }
} 