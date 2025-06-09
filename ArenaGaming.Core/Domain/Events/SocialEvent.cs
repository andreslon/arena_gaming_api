using System;

namespace ArenaGaming.Core.Domain.Events;

public class SocialEvent
{
    public Guid UserId { get; }
    public SocialEventType EventType { get; }
    public Guid SourceUserId { get; }
    public DateTime Timestamp { get; }

    public SocialEvent(Guid userId, SocialEventType eventType, Guid sourceUserId)
    {
        UserId = userId;
        EventType = eventType;
        SourceUserId = sourceUserId;
        Timestamp = DateTime.UtcNow;
    }
}

public enum SocialEventType
{
    FriendRequest,
    FriendRequestAccepted,
    GameInvitation
} 