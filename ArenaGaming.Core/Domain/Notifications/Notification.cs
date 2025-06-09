using ArenaGaming.Core.Domain.Common;

namespace ArenaGaming.Core.Domain.Notifications;

public class Notification : Entity
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum NotificationType
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Success = 3,
    GameUpdate = 4,
    TournamentAlert = 5,
    PlayerAction = 6
} 