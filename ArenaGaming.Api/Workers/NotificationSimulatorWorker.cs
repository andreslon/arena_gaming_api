using ArenaGaming.Api.Services;
using ArenaGaming.Core.Domain.Notifications;

namespace ArenaGaming.Api.Workers;

public class NotificationSimulatorWorker : BackgroundService
{
    private readonly ILogger<NotificationSimulatorWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Random _random = new();

    public NotificationSimulatorWorker(
        ILogger<NotificationSimulatorWorker> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationSimulatorWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<IPulsarNotificationService>();

                // Simulate different types of notifications
                await SimulateRandomNotification(notificationService);

                // Wait for a random interval between 5-30 seconds
                var delay = TimeSpan.FromSeconds(_random.Next(5, 31));
                await Task.Delay(delay, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NotificationSimulatorWorker");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task SimulateRandomNotification(IPulsarNotificationService notificationService)
    {
        var notificationTypes = Enum.GetValues<NotificationType>();
        var randomType = notificationTypes[_random.Next(notificationTypes.Length)];

        var notification = randomType switch
        {
            NotificationType.GameUpdate => CreateGameUpdateNotification(),
            NotificationType.TournamentAlert => CreateTournamentAlertNotification(),
            NotificationType.PlayerAction => CreatePlayerActionNotification(),
            NotificationType.Success => CreateSuccessNotification(),
            NotificationType.Warning => CreateWarningNotification(),
            NotificationType.Error => CreateErrorNotification(),
            _ => CreateInfoNotification()
        };

        // Randomly decide if it's a user-specific or broadcast notification
        var shouldBroadcast = _random.Next(1, 5) == 1; // 20% chance for broadcast

        if (shouldBroadcast)
        {
            await notificationService.SendBroadcastNotificationAsync(notification);
            _logger.LogInformation("Simulated broadcast notification: {Type}", notification.Type);
        }
        else
        {
            var userId = GenerateRandomUserId();
            notification.UserId = userId;
            await notificationService.SendNotificationToUserAsync(userId, notification);
            _logger.LogInformation("Simulated user notification for {UserId}: {Type}", userId, notification.Type);
        }
    }

    private Notification CreateGameUpdateNotification()
    {
        var gameEvents = new[]
        {
            "New game match found!",
            "Your opponent has made a move",
            "Game round completed",
            "Match result updated",
            "Your ranking has changed"
        };

        return new Notification
        {
            Title = "Game Update",
            Message = gameEvents[_random.Next(gameEvents.Length)],
            Type = NotificationType.GameUpdate,
            Metadata = new Dictionary<string, object>
            {
                ["gameId"] = Guid.NewGuid().ToString(),
                ["round"] = _random.Next(1, 10)
            }
        };
    }

    private Notification CreateTournamentAlertNotification()
    {
        var tournamentEvents = new[]
        {
            "New tournament starting soon!",
            "Tournament bracket updated",
            "Registration deadline approaching",
            "Tournament results are now available",
            "Prize distribution completed"
        };

        return new Notification
        {
            Title = "Tournament Alert",
            Message = tournamentEvents[_random.Next(tournamentEvents.Length)],
            Type = NotificationType.TournamentAlert,
            Metadata = new Dictionary<string, object>
            {
                ["tournamentId"] = Guid.NewGuid().ToString(),
                ["participants"] = _random.Next(10, 100)
            }
        };
    }

    private Notification CreatePlayerActionNotification()
    {
        var playerActions = new[]
        {
            "Friend request received",
            "You have been challenged to a match",
            "Team invitation received",
            "New message from opponent",
            "Achievement unlocked!"
        };

        return new Notification
        {
            Title = "Player Action",
            Message = playerActions[_random.Next(playerActions.Length)],
            Type = NotificationType.PlayerAction,
            Metadata = new Dictionary<string, object>
            {
                ["playerId"] = Guid.NewGuid().ToString(),
                ["action"] = "challenge"
            }
        };
    }

    private Notification CreateSuccessNotification()
    {
        var successMessages = new[]
        {
            "Profile updated successfully",
            "Payment processed successfully",
            "Settings saved",
            "Account verified",
            "Data synchronized"
        };

        return new Notification
        {
            Title = "Success",
            Message = successMessages[_random.Next(successMessages.Length)],
            Type = NotificationType.Success
        };
    }

    private Notification CreateWarningNotification()
    {
        var warningMessages = new[]
        {
            "Low account balance",
            "Password expires soon",
            "Maintenance scheduled",
            "Connection unstable",
            "Update available"
        };

        return new Notification
        {
            Title = "Warning",
            Message = warningMessages[_random.Next(warningMessages.Length)],
            Type = NotificationType.Warning
        };
    }

    private Notification CreateErrorNotification()
    {
        var errorMessages = new[]
        {
            "Failed to save data",
            "Connection lost",
            "Invalid operation",
            "Service temporarily unavailable",
            "Authentication failed"
        };

        return new Notification
        {
            Title = "Error",
            Message = errorMessages[_random.Next(errorMessages.Length)],
            Type = NotificationType.Error
        };
    }

    private Notification CreateInfoNotification()
    {
        var infoMessages = new[]
        {
            "System maintenance completed",
            "New feature available",
            "Welcome to Arena Gaming!",
            "Daily bonus available",
            "Community event starting"
        };

        return new Notification
        {
            Title = "Information",
            Message = infoMessages[_random.Next(infoMessages.Length)],
            Type = NotificationType.Info
        };
    }

    private string GenerateRandomUserId()
    {
        var userIds = new[]
        {
            "user_001", "user_002", "user_003", "user_004", "user_005",
            "user_006", "user_007", "user_008", "user_009", "user_010"
        };
        
        return userIds[_random.Next(userIds.Length)];
    }
} 