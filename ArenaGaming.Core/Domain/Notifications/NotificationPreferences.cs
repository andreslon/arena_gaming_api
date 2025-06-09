using ArenaGaming.Core.Domain.Common;

namespace ArenaGaming.Core.Domain.Notifications;

public class NotificationPreferences : Entity
{
    public string UserId { get; set; } = string.Empty;
    public bool GameEvents { get; set; } = true;
    public bool SocialEvents { get; set; } = true;
    public bool SoundEffects { get; set; } = true;
    public int Volume { get; set; } = 50; // Default volume at 50%
    
    // Additional preference fields
    public bool EmailNotifications { get; set; } = false;
    public bool PushNotifications { get; set; } = true;
    
    // Notification type preferences
    public bool TournamentAlerts { get; set; } = true;
    public bool PlayerActions { get; set; } = true;
    public bool SystemUpdates { get; set; } = true;
    
    public void UpdatePreferences(bool gameEvents, bool socialEvents, bool soundEffects, int volume)
    {
        GameEvents = gameEvents;
        SocialEvents = socialEvents;
        SoundEffects = soundEffects;
        Volume = Math.Clamp(volume, 0, 100); // Ensure volume is between 0-100
    }
} 