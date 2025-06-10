using ArenaGaming.Core.Domain.Notifications;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json;

namespace ArenaGaming.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationPreferencesController : ControllerBase
{
    private readonly IDatabase _database;
    private readonly ILogger<NotificationPreferencesController> _logger;
    private const string PREFERENCES_KEY_PREFIX = "notification_preferences:";

    public NotificationPreferencesController(
        IConnectionMultiplexer redis,
        ILogger<NotificationPreferencesController> logger)
    {
        _database = redis.GetDatabase();
        _logger = logger;
    }

    /// <summary>
    /// Get notification preferences for a user
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetPreferences(string userId)
    {
        try
        {
            var key = PREFERENCES_KEY_PREFIX + userId;
            var preferencesJson = await _database.StringGetAsync(key);

            NotificationPreferences preferences;

            if (!preferencesJson.HasValue)
            {
                // Create default preferences if none exist
                preferences = new NotificationPreferences
                {
                    UserId = userId,
                    GameEvents = true,
                    SocialEvents = true,
                    SoundEffects = true,
                    Volume = 50
                };

                var json = JsonSerializer.Serialize(preferences);
                await _database.StringSetAsync(key, json, TimeSpan.FromDays(30));
            }
            else
            {
                preferences = JsonSerializer.Deserialize<NotificationPreferences>(preferencesJson!)!;
            }

            return Ok(new NotificationPreferencesResponse
            {
                UserId = preferences.UserId,
                GameEvents = preferences.GameEvents,
                SocialEvents = preferences.SocialEvents,
                SoundEffects = preferences.SoundEffects,
                Volume = preferences.Volume,
                EmailNotifications = preferences.EmailNotifications,
                PushNotifications = preferences.PushNotifications,
                TournamentAlerts = preferences.TournamentAlerts,
                PlayerActions = preferences.PlayerActions,
                SystemUpdates = preferences.SystemUpdates
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preferences for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to get notification preferences" });
        }
    }

    /// <summary>
    /// Update notification preferences for a user
    /// </summary>
    [HttpPut("{userId}")]
    public async Task<IActionResult> UpdatePreferences(string userId, [FromBody] UpdateNotificationPreferencesRequest request)
    {
        try
        {
            var key = PREFERENCES_KEY_PREFIX + userId;
            var preferencesJson = await _database.StringGetAsync(key);

            NotificationPreferences preferences;

            if (!preferencesJson.HasValue)
            {
                preferences = new NotificationPreferences
                {
                    UserId = userId
                };
            }
            else
            {
                preferences = JsonSerializer.Deserialize<NotificationPreferences>(preferencesJson!)!;
            }

            // Update preferences
            preferences.UpdatePreferences(
                request.GameEvents,
                request.SocialEvents,
                request.SoundEffects,
                request.Volume
            );

            // Update additional preferences if provided
            if (request.EmailNotifications.HasValue)
                preferences.EmailNotifications = request.EmailNotifications.Value;
            if (request.PushNotifications.HasValue)
                preferences.PushNotifications = request.PushNotifications.Value;
            if (request.TournamentAlerts.HasValue)
                preferences.TournamentAlerts = request.TournamentAlerts.Value;
            if (request.PlayerActions.HasValue)
                preferences.PlayerActions = request.PlayerActions.Value;
            if (request.SystemUpdates.HasValue)
                preferences.SystemUpdates = request.SystemUpdates.Value;

            var updatedJson = JsonSerializer.Serialize(preferences);
            await _database.StringSetAsync(key, updatedJson, TimeSpan.FromDays(30));

            return Ok(new { success = true, message = "Preferences updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating preferences for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to update notification preferences" });
        }
    }

    /// <summary>
    /// Reset notification preferences to default values
    /// </summary>
    [HttpPost("{userId}/reset")]
    public async Task<IActionResult> ResetPreferences(string userId)
    {
        try
        {
            var preferences = new NotificationPreferences
            {
                UserId = userId,
                GameEvents = true,
                SocialEvents = true,
                SoundEffects = true,
                Volume = 50,
                EmailNotifications = false,
                PushNotifications = true,
                TournamentAlerts = true,
                PlayerActions = true,
                SystemUpdates = true
            };

            var key = PREFERENCES_KEY_PREFIX + userId;
            var json = JsonSerializer.Serialize(preferences);
            await _database.StringSetAsync(key, json, TimeSpan.FromDays(30));

            return Ok(new { success = true, message = "Preferences reset to default values" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting preferences for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to reset notification preferences" });
        }
    }
}

public class NotificationPreferencesResponse
{
    public string UserId { get; set; } = string.Empty;
    public bool GameEvents { get; set; }
    public bool SocialEvents { get; set; }
    public bool SoundEffects { get; set; }
    public int Volume { get; set; }
    public bool EmailNotifications { get; set; }
    public bool PushNotifications { get; set; }
    public bool TournamentAlerts { get; set; }
    public bool PlayerActions { get; set; }
    public bool SystemUpdates { get; set; }
}

public class UpdateNotificationPreferencesRequest
{
    public bool GameEvents { get; set; }
    public bool SocialEvents { get; set; }
    public bool SoundEffects { get; set; }
    public int Volume { get; set; }
    public bool? EmailNotifications { get; set; }
    public bool? PushNotifications { get; set; }
    public bool? TournamentAlerts { get; set; }
    public bool? PlayerActions { get; set; }
    public bool? SystemUpdates { get; set; }
} 