using System;
using ArenaGaming.Core.Domain.Common;

namespace ArenaGaming.Core.Domain;

public class Move : Entity
{
    public Guid GameId { get; private set; }
    public int Position { get; private set; }
    public char Symbol { get; private set; }
    public Guid? PlayerId { get; private set; }
    public DateTime Timestamp { get; private set; }

    private Move() { } // For EF Core

    public Move(Guid gameId, int position, char symbol, Guid? playerId)
    {
        GameId = gameId;
        Position = position;
        Symbol = symbol;
        PlayerId = playerId;
        Timestamp = DateTime.UtcNow;
    }
} 