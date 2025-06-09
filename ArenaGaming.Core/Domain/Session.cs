using System;
using ArenaGaming.Core.Domain.Common;

namespace ArenaGaming.Core.Domain;

public class Session : Entity
{
    public Guid PlayerId { get; private set; }
    public Guid? CurrentGameId { get; set; }
    public DateTime CreatedAt { get; private set; }
    public SessionStatus Status { get; private set; }
    public DateTime? EndedAt { get; private set; }

    private Session() { } // For EF Core

    public Session(Guid playerId)
    {
        PlayerId = playerId;
        CreatedAt = DateTime.UtcNow;
    }

    public void End()
    {
        if (Status != SessionStatus.InProgress)
            throw new InvalidOperationException("Session is not in progress");

        Status = SessionStatus.Ended;
        EndedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum SessionStatus
{
    InProgress,
    Ended
} 