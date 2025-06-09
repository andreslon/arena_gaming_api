using System.Threading;
using System.Threading.Tasks;

namespace ArenaGaming.Core.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, string topic, CancellationToken cancellationToken = default) where T : class;
} 