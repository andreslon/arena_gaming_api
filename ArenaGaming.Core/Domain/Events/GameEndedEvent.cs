using System;

namespace ArenaGaming.Core.Domain.Events;

public class GameEndedEvent
{
    public Guid GameId { get; }
    public Guid? WinnerId { get; }
    public bool IsDraw { get; }
    public DateTime Timestamp { get; }

    public GameEndedEvent(Guid gameId, Guid? winnerId, bool isDraw)
    {
        GameId = gameId;
        WinnerId = winnerId;
        IsDraw = isDraw;
        Timestamp = DateTime.UtcNow;
    }
} 