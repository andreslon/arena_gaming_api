using System;
using System.Threading;
using System.Threading.Tasks;
using ArenaGaming.Core.Application.Interfaces;
using ArenaGaming.Core.Domain.Events;
using Microsoft.AspNetCore.Mvc;
using ArenaGaming.Api.Services;
using ArenaGaming.Core.Domain.Notifications;

namespace ArenaGaming.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly IPulsarNotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        IPulsarNotificationService notificationService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Send a notification to a specific user
    /// </summary>
    [HttpPost("user/{userId}")]
    public async Task<IActionResult> SendNotificationToUser(string userId, [FromBody] CreateNotificationRequest request)
    {
        try
        {
            var notification = new Notification
            {
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                UserId = userId,
                Metadata = request.Metadata ?? new Dictionary<string, object>()
            };

            await _notificationService.SendNotificationToUserAsync(userId, notification);

            return Ok(new { success = true, notificationId = notification.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to send notification" });
        }
    }

    /// <summary>
    /// Send a broadcast notification to all connected users
    /// </summary>
    [HttpPost("broadcast")]
    public async Task<IActionResult> SendBroadcastNotification([FromBody] CreateNotificationRequest request)
    {
        try
        {
            var notification = new Notification
            {
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                Metadata = request.Metadata ?? new Dictionary<string, object>()
            };

            await _notificationService.SendBroadcastNotificationAsync(notification);

            return Ok(new { success = true, notificationId = notification.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending broadcast notification");
            return StatusCode(500, new { error = "Failed to send broadcast notification" });
        }
    }

    /// <summary>
    /// Send a notification to Pulsar queue for processing
    /// </summary>
    [HttpPost("queue")]
    public async Task<IActionResult> QueueNotification([FromBody] CreateNotificationRequest request)
    {
        try
        {
            var notification = new Notification
            {
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                UserId = request.UserId ?? string.Empty,
                Metadata = request.Metadata ?? new Dictionary<string, object>()
            };

            await _notificationService.SendNotificationAsync(notification);

            return Ok(new { success = true, notificationId = notification.Id, status = "queued" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queuing notification");
            return StatusCode(500, new { error = "Failed to queue notification" });
        }
    }

    /// <summary>
    /// Send a test notification for debugging
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> SendTestNotification([FromQuery] string? userId = null)
    {
        try
        {
            var notification = new Notification
            {
                Title = "Test Notification",
                Message = $"This is a test notification sent at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
                Type = NotificationType.Info,
                UserId = userId ?? string.Empty
            };

            if (!string.IsNullOrEmpty(userId))
            {
                await _notificationService.SendNotificationToUserAsync(userId, notification);
            }
            else
            {
                await _notificationService.SendBroadcastNotificationAsync(notification);
            }

            return Ok(new 
            { 
                success = true, 
                notificationId = notification.Id,
                message = "Test notification sent successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test notification");
            return StatusCode(500, new { error = "Failed to send test notification" });
        }
    }
}

public class CreateNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public string? UserId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class NotificationRequest
{
    public required string Message { get; set; }
    public required string Category { get; set; }
}

public class SendSocialEventRequest
{
    public Guid UserId { get; set; }
    public SocialEventType EventType { get; set; }
    public Guid SourceUserId { get; set; }
} 