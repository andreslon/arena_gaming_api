using System;

namespace ArenaGaming.Core.Domain.Events;

public class MoveMadeEvent
{
    public Guid GameId { get; }
    public Guid PlayerId { get; }
    public int Position { get; }
    public string Board { get; }
    public DateTime Timestamp { get; }

    public MoveMadeEvent(Guid gameId, Guid playerId, int position, string board)
    {
        GameId = gameId;
        PlayerId = playerId;
        Position = position;
        Board = board;
        Timestamp = DateTime.UtcNow;
    }
} 