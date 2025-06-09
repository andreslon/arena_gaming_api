using System.Threading;
using System.Threading.Tasks;
using ArenaGaming.Core.Domain.Events;

namespace ArenaGaming.Core.Application.Interfaces;

public interface INotificationService
{
    Task SendNotificationAsync(NotificationMessage notification, CancellationToken cancellationToken = default);
    Task HandleGameEventAsync(GameEndedEvent @event, CancellationToken cancellationToken = default);
    Task HandleMoveEventAsync(MoveMadeEvent @event, CancellationToken cancellationToken = default);
    Task HandleSocialEventAsync(SocialEvent @event, CancellationToken cancellationToken = default);
} 