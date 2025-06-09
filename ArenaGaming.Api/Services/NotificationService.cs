using ArenaGaming.Core.Domain.Notifications;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using System.Text.Json;

namespace ArenaGaming.Api.Services;

public interface IPulsarNotificationService
{
    Task SendNotificationAsync(Notification notification);
    Task SendNotificationToUserAsync(string userId, Notification notification);
    Task SendBroadcastNotificationAsync(Notification notification);
    Task SendNotificationToTopicAsync(string topic, Notification notification);
}

public class PulsarNotificationService : IPulsarNotificationService
{
    private readonly IPulsarClient _pulsarClient;
    private readonly ILogger<PulsarNotificationService> _logger;

    public PulsarNotificationService(
        IPulsarClient pulsarClient,
        ILogger<PulsarNotificationService> logger)
    {
        _pulsarClient = pulsarClient;
        _logger = logger;
    }

    public async Task SendNotificationAsync(Notification notification)
    {
        try
        {
            var producer = _pulsarClient.NewProducer()
                .Topic("notifications")
                .Create();

            var notificationJson = JsonSerializer.Serialize(notification);
            var messageBytes = System.Text.Encoding.UTF8.GetBytes(notificationJson);
            
            await producer.Send(messageBytes);
            await producer.DisposeAsync();
            
            _logger.LogInformation("Notification sent to Pulsar topic 'notifications': {NotificationId}", notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to Pulsar: {NotificationId}", notification.Id);
            throw;
        }
    }

    public async Task SendNotificationToUserAsync(string userId, Notification notification)
    {
        try
        {
            // Send to user-specific topic
            var userTopic = $"user-notifications-{userId}";
            await SendNotificationToTopicAsync(userTopic, notification);
            
            _logger.LogInformation("Notification sent to user {UserId} topic: {NotificationId}", userId, notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}: {NotificationId}", userId, notification.Id);
            throw;
        }
    }

    public async Task SendBroadcastNotificationAsync(Notification notification)
    {
        try
        {
            // Send to broadcast topic
            await SendNotificationToTopicAsync("broadcast-notifications", notification);
            
            _logger.LogInformation("Broadcast notification sent: {NotificationId}", notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending broadcast notification: {NotificationId}", notification.Id);
            throw;
        }
    }

    public async Task SendNotificationToTopicAsync(string topic, Notification notification)
    {
        try
        {
            var producer = _pulsarClient.NewProducer()
                .Topic(topic)
                .Create();

            var notificationJson = JsonSerializer.Serialize(notification);
            var messageBytes = System.Text.Encoding.UTF8.GetBytes(notificationJson);
            
            await producer.Send(messageBytes);
            await producer.DisposeAsync();
            
            _logger.LogInformation("Notification sent to topic '{Topic}': {NotificationId}", topic, notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to topic '{Topic}': {NotificationId}", topic, notification.Id);
            throw;
        }
    }
} 