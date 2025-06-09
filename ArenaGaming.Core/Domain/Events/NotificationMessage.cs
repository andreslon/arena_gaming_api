using System;

namespace ArenaGaming.Core.Domain.Events;

public class NotificationMessage
{
    public Guid UserId { get; }
    public string Message { get; }
    public string Category { get; }
    public DateTime Timestamp { get; }

    public NotificationMessage(Guid userId, string message, string category)
    {
        UserId = userId;
        Message = message;
        Category = category;
        Timestamp = DateTime.UtcNow;
    }
} 