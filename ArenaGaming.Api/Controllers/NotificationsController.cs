using System;
using System.Threading;
using System.Threading.Tasks;
using ArenaGaming.Core.Application.Interfaces;
using ArenaGaming.Core.Domain.Events;
using Microsoft.AspNetCore.Mvc;

namespace ArenaGaming.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendNotification(
        [FromBody] NotificationMessage notification,
        CancellationToken cancellationToken)
    {
        await _notificationService.SendNotificationAsync(notification, cancellationToken);
        return Ok();
    }

    [HttpPost("social")]
    public async Task<IActionResult> SendSocialEvent(
        [FromBody] SocialEvent socialEvent,
        CancellationToken cancellationToken)
    {
        await _notificationService.HandleSocialEventAsync(socialEvent, cancellationToken);
        return Ok();
    }
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