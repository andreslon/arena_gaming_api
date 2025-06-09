using System;

namespace ArenaGaming.Core.Domain.Events;

public class GeminiMoveEvent
{
    public Guid SessionId { get; }
    public int Position { get; }
    public string AiResponseRaw { get; }
    public DateTime Timestamp { get; }

    public GeminiMoveEvent(Guid sessionId, int position, string aiResponseRaw)
    {
        SessionId = sessionId;
        Position = position;
        AiResponseRaw = aiResponseRaw;
        Timestamp = DateTime.UtcNow;
    }
} 