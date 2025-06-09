using System;
using System.Threading;
using System.Threading.Tasks;
using ArenaGaming.Core.Application.Interfaces;
using ArenaGaming.Core.Domain.Events;

namespace ArenaGaming.Core.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IEventPublisher _eventPublisher;

    public NotificationService(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    public async Task SendNotificationAsync(Guid userId, string message, string category, CancellationToken cancellationToken = default)
    {
        var notification = new NotificationMessage(userId, message, category);
        await _eventPublisher.PublishAsync(notification, "notifications", cancellationToken);
    }

    public async Task SendNotificationAsync(NotificationMessage notification, CancellationToken cancellationToken = default)
    {
        await _eventPublisher.PublishAsync(notification, "notifications", cancellationToken);
    }

    public async Task HandleGameEventAsync(GameStartedEvent gameEvent, CancellationToken cancellationToken = default)
    {
        var message = $"Game {gameEvent.GameId} has started!";
        await SendNotificationAsync(gameEvent.PlayerId, message, "game", cancellationToken);
    }

    public async Task HandleGameEventAsync(GameEndedEvent gameEvent, CancellationToken cancellationToken = default)
    {
        if (gameEvent.WinnerId != null)
        {
            var message = $"Game {gameEvent.GameId} has ended. You won!";
            await SendNotificationAsync(gameEvent.WinnerId.Value, message, "game", cancellationToken);
        }
        else if (gameEvent.IsDraw)
        {
            var message = $"Game {gameEvent.GameId} has ended in a draw.";
            await SendNotificationAsync(gameEvent.GameId, message, "game", cancellationToken);
        }
    }

    public async Task HandleMoveEventAsync(MoveMadeEvent moveEvent, CancellationToken cancellationToken = default)
    {
        if (moveEvent.PlayerId != Guid.Empty)
        {
            var message = $"Player made a move at position {moveEvent.Position} in game {moveEvent.GameId}";
            await SendNotificationAsync(moveEvent.PlayerId, message, "move", cancellationToken);
        }
    }

    public async Task HandleSocialEventAsync(SocialEvent socialEvent, CancellationToken cancellationToken = default)
    {
        var message = socialEvent.EventType switch
        {
            SocialEventType.FriendRequest => $"You have a new friend request from user {socialEvent.SourceUserId}",
            SocialEventType.FriendRequestAccepted => $"User {socialEvent.SourceUserId} accepted your friend request",
            SocialEventType.GameInvitation => $"You have been invited to play a game by user {socialEvent.SourceUserId}",
            _ => throw new ArgumentException($"Unknown social event type: {socialEvent.EventType}")
        };

        await SendNotificationAsync(socialEvent.UserId, message, "social", cancellationToken);
    }
} 