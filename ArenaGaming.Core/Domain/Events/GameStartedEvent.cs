using System;

namespace ArenaGaming.Core.Domain.Events;

public class GameStartedEvent
{
    public Guid GameId { get; }
    public Guid PlayerId { get; }
    public DateTime Timestamp { get; }

    public GameStartedEvent(Guid gameId, Guid playerId)
    {
        GameId = gameId;
        PlayerId = playerId;
        Timestamp = DateTime.UtcNow;
    }
} 