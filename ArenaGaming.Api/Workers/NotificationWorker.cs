using ArenaGaming.Core.Domain.Notifications;
using ArenaGaming.Api.Services;
using DotPulsar;
using DotPulsar.Extensions;
using DotPulsar.Abstractions;
using System.Buffers;
using System.Text.Json;

namespace ArenaGaming.Api.Workers;

public class NotificationWorker : BackgroundService
{
    private readonly ILogger<NotificationWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPulsarClient _pulsarClient;
    private IConsumer<ReadOnlySequence<byte>>? _consumer;

    public NotificationWorker(
        ILogger<NotificationWorker> logger,
        IServiceProvider serviceProvider,
        IPulsarClient pulsarClient)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _pulsarClient = pulsarClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Initialize Pulsar consumer
            _consumer = _pulsarClient.NewConsumer()
                .SubscriptionName("notification-worker")
                .Topic("notifications")
                .SubscriptionType(SubscriptionType.Shared)
                .Create();

            _logger.LogInformation("NotificationWorker started and listening for messages...");

            await foreach (var message in _consumer.Messages(stoppingToken))
            {
                try
                {
                    await ProcessNotificationMessage(message);
                    await _consumer.Acknowledge(message);
                    _logger.LogDebug("Message processed and acknowledged: {MessageId}", message.MessageId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message: {MessageId}", message.MessageId);
                    // In a production scenario, you might want to implement retry logic or dead letter queue
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NotificationWorker encountered a fatal error");
            throw;
        }
    }

    private async Task ProcessNotificationMessage(IMessage<ReadOnlySequence<byte>> message)
    {
        try
        {
            var messageContent = System.Text.Encoding.UTF8.GetString(message.Data.ToArray());
            var notification = JsonSerializer.Deserialize<Notification>(messageContent);

            if (notification == null)
            {
                _logger.LogWarning("Failed to deserialize notification message: {MessageId}", message.MessageId);
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<IPulsarNotificationService>();

            // Process different types of notifications by forwarding to appropriate topics
            await ProcessNotificationByType(notification, notificationService);

            _logger.LogInformation("Processed notification: {NotificationId} of type {Type}", 
                notification.Id, notification.Type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification message: {MessageId}", message.MessageId);
            throw;
        }
    }

    private async Task ProcessNotificationByType(Notification notification, IPulsarNotificationService notificationService)
    {
        switch (notification.Type)
        {
            case NotificationType.GameUpdate:
                await ProcessGameUpdateNotification(notification, notificationService);
                break;
            case NotificationType.TournamentAlert:
                await ProcessTournamentAlertNotification(notification, notificationService);
                break;
            case NotificationType.PlayerAction:
                await ProcessPlayerActionNotification(notification, notificationService);
                break;
            default:
                await ProcessGenericNotification(notification, notificationService);
                break;
        }
    }

    private async Task ProcessGameUpdateNotification(Notification notification, IPulsarNotificationService notificationService)
    {
        // Send to specific user topic if UserId is provided
        if (!string.IsNullOrEmpty(notification.UserId))
        {
            await notificationService.SendNotificationToTopicAsync($"game-updates-{notification.UserId}", notification);
        }
        else
        {
            // Send to general game updates topic
            await notificationService.SendNotificationToTopicAsync("game-updates", notification);
        }
    }

    private async Task ProcessTournamentAlertNotification(Notification notification, IPulsarNotificationService notificationService)
    {
        // Tournament alerts are usually broadcast to all users
        await notificationService.SendNotificationToTopicAsync("tournament-alerts", notification);
    }

    private async Task ProcessPlayerActionNotification(Notification notification, IPulsarNotificationService notificationService)
    {
        // Player actions are sent to specific users
        if (!string.IsNullOrEmpty(notification.UserId))
        {
            await notificationService.SendNotificationToTopicAsync($"player-actions-{notification.UserId}", notification);
        }
    }

    private async Task ProcessGenericNotification(Notification notification, IPulsarNotificationService notificationService)
    {
        // Generic notifications
        if (!string.IsNullOrEmpty(notification.UserId))
        {
            await notificationService.SendNotificationToTopicAsync($"user-notifications-{notification.UserId}", notification);
        }
        else
        {
            await notificationService.SendNotificationToTopicAsync("broadcast-notifications", notification);
        }
    }

    public override void Dispose()
    {
        _consumer?.DisposeAsync();
        base.Dispose();
    }
} 