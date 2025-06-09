using ArenaGaming.Core.Domain.Notifications;
using ArenaGaming.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArenaGaming.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationPreferencesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationPreferencesController> _logger;

    public NotificationPreferencesController(
        ApplicationDbContext context,
        ILogger<NotificationPreferencesController> logger)
    {
        _context = context;
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
            var preferences = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (preferences == null)
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

                _context.NotificationPreferences.Add(preferences);
                await _context.SaveChangesAsync();
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
            var preferences = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (preferences == null)
            {
                preferences = new NotificationPreferences
                {
                    UserId = userId
                };
                _context.NotificationPreferences.Add(preferences);
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

            await _context.SaveChangesAsync();

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
            var preferences = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (preferences == null)
            {
                preferences = new NotificationPreferences
                {
                    UserId = userId
                };
                _context.NotificationPreferences.Add(preferences);
            }
            else
            {
                // Reset to default values
                preferences.GameEvents = true;
                preferences.SocialEvents = true;
                preferences.SoundEffects = true;
                preferences.Volume = 50;
                preferences.EmailNotifications = false;
                preferences.PushNotifications = true;
                preferences.TournamentAlerts = true;
                preferences.PlayerActions = true;
                preferences.SystemUpdates = true;
            }

            await _context.SaveChangesAsync();

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