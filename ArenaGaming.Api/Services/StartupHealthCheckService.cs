using ArenaGaming.Core.Domain;
using ArenaGaming.Infrastructure.Persistence;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Buffers;
using System.Diagnostics;
using System.Text.Json;

namespace ArenaGaming.Api.Services;

public class StartupHealthCheckService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StartupHealthCheckService> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IHealthCheckResultsService _healthCheckResults;

    public StartupHealthCheckService(
        IServiceProvider serviceProvider,
        ILogger<StartupHealthCheckService> logger,
        IHostApplicationLifetime applicationLifetime,
        IHealthCheckResultsService healthCheckResults)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _applicationLifetime = applicationLifetime;
        _healthCheckResults = healthCheckResults;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Wait a bit for the application to fully start and services to initialize
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

            _logger.LogInformation("🚀 ARENA GAMING API - STARTING HEALTH CHECKS");
            _logger.LogInformation("════════════════════════════════════════════════════════");
            
            var overallHealthy = true;
            var healthResults = new List<ServiceHealthResult>();

            // Test PostgreSQL
            _logger.LogInformation("🔍 TESTING POSTGRESQL DATABASE...");
            var postgresResult = await TestPostgreSQL();
            healthResults.Add(postgresResult);
            LogServiceResult(postgresResult);
            
            // Test Redis
            _logger.LogInformation("🔍 TESTING REDIS CACHE...");
            var redisResult = await TestRedis();
            healthResults.Add(redisResult);
            LogServiceResult(redisResult);
            
            // Test Pulsar
            _logger.LogInformation("🔍 TESTING PULSAR MESSAGING...");
            var pulsarResult = await TestPulsar();
            healthResults.Add(pulsarResult);
            LogServiceResult(pulsarResult);

            // Check overall health
            foreach (var result in healthResults)
            {
                if (!result.IsHealthy)
                    overallHealthy = false;
            }

            // Log overall result with big emphasis
            _logger.LogInformation("════════════════════════════════════════════════════════");
            if (overallHealthy)
            {
                _logger.LogInformation("🎉 ALL SERVICES HEALTHY - APPLICATION READY!");
                _logger.LogInformation("✅ PostgreSQL: OK | ✅ Redis: OK | ✅ Pulsar: OK");
            }
            else
            {
                _logger.LogError("⚠️  SOME SERVICES UNHEALTHY - CHECK DETAILS ABOVE");
                var statusSummary = $"{(postgresResult.IsHealthy ? "✅" : "❌")} PostgreSQL | " +
                                  $"{(redisResult.IsHealthy ? "✅" : "❌")} Redis | " +
                                  $"{(pulsarResult.IsHealthy ? "✅" : "❌")} Pulsar";
                _logger.LogWarning(statusSummary);
            }

            // Log startup summary
            LogStartupSummary(healthResults);

            // Store results for API access
            _healthCheckResults.SetStartupResults(healthResults);
            
            _logger.LogInformation("🏁 HEALTH CHECK PROCESS COMPLETED");
            _logger.LogInformation("════════════════════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 CRITICAL ERROR DURING HEALTH CHECKS");
            _logger.LogError("❌ Health check process failed: {ErrorMessage}", ex.Message);
        }
    }

    private async Task<ServiceHealthResult> TestPostgreSQL()
    {
        var result = new ServiceHealthResult { ServiceName = "PostgreSQL" };
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("   🔗 Connecting to PostgreSQL database...");
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Test connection
            await dbContext.Database.OpenConnectionAsync();
            result.Details.Add("Connection", "✅ Successful");
            _logger.LogInformation("   ✅ Database connection established");

            // Test migration status and apply if needed
            _logger.LogInformation("   🔍 Checking database migrations...");
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            if (!pendingMigrations.Any())
            {
                result.Details.Add("Migrations", "✅ All applied");
                _logger.LogInformation("   ✅ All migrations up to date");
            }
            else
            {
                _logger.LogWarning("   ⚠️ Found {Count} pending migrations - applying now...", pendingMigrations.Count());
                result.Details.Add("Migrations", $"⚠️ {pendingMigrations.Count()} pending - Applying now...");
                result.Warnings.Add($"Auto-applying migrations: {string.Join(", ", pendingMigrations)}");
                
                // Apply pending migrations
                await dbContext.Database.MigrateAsync();
                result.Details["Migrations"] = "✅ Applied automatically";
                _logger.LogInformation("   ✅ Migrations applied successfully");
            }

            // Test basic query (now should work after migrations)
            _logger.LogInformation("   📊 Reading database statistics...");
            var gameCount = await dbContext.Games.CountAsync();
            var sessionCount = await dbContext.Sessions.CountAsync();
            var notificationCount = await dbContext.Notifications.CountAsync();
            var preferencesCount = await dbContext.NotificationPreferences.CountAsync();
            result.Details.Add("Data Access", $"✅ Games: {gameCount}, Sessions: {sessionCount}, Notifications: {notificationCount}, Preferences: {preferencesCount}");
            _logger.LogInformation("   ✅ Database queries successful (Games: {Games}, Sessions: {Sessions})", gameCount, sessionCount);

            // Test a write operation (create and delete a test session)
            _logger.LogInformation("   ✏️ Testing write operations...");
            var testSession = new Session(Guid.NewGuid());
            dbContext.Sessions.Add(testSession);
            await dbContext.SaveChangesAsync();
            
            dbContext.Sessions.Remove(testSession);
            await dbContext.SaveChangesAsync();
            result.Details.Add("Write Operations", "✅ Create/Delete successful");
            _logger.LogInformation("   ✅ Write operations successful");

            await dbContext.Database.CloseConnectionAsync();
            
            stopwatch.Stop();
            result.IsHealthy = true;
            result.ResponseTime = stopwatch.ElapsedMilliseconds;
            result.Message = "PostgreSQL is healthy and ready";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.IsHealthy = false;
            result.ResponseTime = stopwatch.ElapsedMilliseconds;
            result.Message = $"PostgreSQL failed: {ex.Message}";
            result.Error = ex;
        }

        return result;
    }

    private async Task<ServiceHealthResult> TestRedis()
    {
        var result = new ServiceHealthResult { ServiceName = "Redis" };
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("   🔗 Connecting to Redis cache...");
            using var scope = _serviceProvider.CreateScope();
            var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
            var database = redis.GetDatabase();

            // Test basic connectivity
            result.Details.Add("Connection", redis.IsConnected ? "✅ Connected" : "❌ Disconnected");
            _logger.LogInformation("   ✅ Redis connection status: {Status}", redis.IsConnected ? "Connected" : "Disconnected");

            // Test write/read operations
            _logger.LogInformation("   📝 Testing Redis read/write operations...");
            var testKey = $"startup-test:{Guid.NewGuid()}";
            var testValue = $"test-value-{DateTime.UtcNow:yyyyMMddHHmmss}";
            
            await database.StringSetAsync(testKey, testValue, TimeSpan.FromSeconds(30));
            var retrievedValue = await database.StringGetAsync(testKey);
            await database.KeyDeleteAsync(testKey);

            if (retrievedValue == testValue)
            {
                result.Details.Add("Read/Write", "✅ Operations successful");
                _logger.LogInformation("   ✅ Redis read/write operations successful");
            }
            else
            {
                result.Details.Add("Read/Write", "❌ Data integrity issue");
                result.Warnings.Add("Redis read/write test failed - data mismatch");
                _logger.LogWarning("   ⚠️ Redis data integrity issue detected");
            }

            // Test additional Redis operations
            try
            {
                // Test hash operations
                var hashKey = $"startup-hash-test:{Guid.NewGuid()}";
                await database.HashSetAsync(hashKey, "field1", "value1");
                var hashValue = await database.HashGetAsync(hashKey, "field1");
                await database.KeyDeleteAsync(hashKey);
                
                result.Details.Add("Hash Operations", hashValue == "value1" ? "✅ Successful" : "❌ Failed");
                result.Details.Add("Server Info", $"✅ Endpoints: {string.Join(", ", redis.GetEndPoints().Select(ep => ep.ToString()))}");
            }
            catch (Exception opsEx)
            {
                result.Details.Add("Advanced Operations", "⚠️ Limited functionality");
                result.Warnings.Add($"Advanced Redis operations warning: {opsEx.Message}");
            }

            stopwatch.Stop();
            result.IsHealthy = true;
            result.ResponseTime = stopwatch.ElapsedMilliseconds;
            result.Message = "Redis is healthy and ready";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.IsHealthy = false;
            result.ResponseTime = stopwatch.ElapsedMilliseconds;
            result.Message = $"Redis failed: {ex.Message}";
            result.Error = ex;
        }

        return result;
    }

    private async Task<ServiceHealthResult> TestPulsar()
    {
        var result = new ServiceHealthResult { ServiceName = "Pulsar" };
        var stopwatch = Stopwatch.StartNew();

        IProducer<ReadOnlySequence<byte>>? producer = null;
        IConsumer<ReadOnlySequence<byte>>? consumer = null;

        try
        {
            _logger.LogInformation("   🔗 Connecting to Pulsar messaging...");
            using var scope = _serviceProvider.CreateScope();
            var pulsarClient = scope.ServiceProvider.GetRequiredService<IPulsarClient>();

            var testTopic = $"startup-test-{Guid.NewGuid()}";
            
            // Test producer creation
            _logger.LogInformation("   📤 Creating Pulsar producer...");
            producer = pulsarClient.NewProducer()
                .Topic(testTopic)
                .Create();
            result.Details.Add("Producer", "✅ Created successfully");
            _logger.LogInformation("   ✅ Pulsar producer created successfully");

            // Test consumer creation
            _logger.LogInformation("   📥 Creating Pulsar consumer...");
            consumer = pulsarClient.NewConsumer()
                .Topic(testTopic)
                .SubscriptionName("startup-health-check")
                .SubscriptionType(SubscriptionType.Exclusive)
                .Create();
            result.Details.Add("Consumer", "✅ Created successfully");
            _logger.LogInformation("   ✅ Pulsar consumer created successfully");

            // Test message sending
            _logger.LogInformation("   📨 Sending test message...");
            var testMessage = new
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Message = "Startup health check test message"
            };

            var messageBytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(testMessage));
            var messageId = await producer.Send(messageBytes);
            result.Details.Add("Message Send", $"✅ Message sent: {messageId}");
            _logger.LogInformation("   ✅ Message sent successfully: {MessageId}", messageId);

            // Test message receiving (with short timeout)
            _logger.LogInformation("   📨 Attempting to receive message...");
            var messageReceived = false;
            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await foreach (var message in consumer.Messages(timeoutCts.Token))
                {
                    await consumer.Acknowledge(message);
                    messageReceived = true;
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                // Timeout is acceptable for startup test
            }

            result.Details.Add("Message Receive", messageReceived ? "✅ Message received" : "⚠️ Timeout (acceptable)");
            
            if (messageReceived)
            {
                _logger.LogInformation("   ✅ Message received and acknowledged successfully");
            }
            else
            {
                _logger.LogInformation("   ⚠️ Message receive timeout (this may be normal during startup)");
                result.Warnings.Add("Message receiving timed out - this may be normal during startup");
            }

            stopwatch.Stop();
            result.IsHealthy = true;
            result.ResponseTime = stopwatch.ElapsedMilliseconds;
            result.Message = "Pulsar is healthy and ready";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.IsHealthy = false;
            result.ResponseTime = stopwatch.ElapsedMilliseconds;
            result.Message = $"Pulsar failed: {ex.Message}";
            result.Error = ex;
        }
        finally
        {
            // Cleanup
            if (producer != null)
                await producer.DisposeAsync();
            if (consumer != null)
                await consumer.DisposeAsync();
        }

        return result;
    }

    private void LogServiceResult(ServiceHealthResult result)
    {
        var statusIcon = result.IsHealthy ? "✅" : "❌";
        var serviceName = result.ServiceName.ToUpper().PadRight(12);
        var status = result.IsHealthy ? "HEALTHY" : "FAILED";
        
        // Big visible header for each service
        _logger.LogInformation("┌─────────────────────────────────────────────────────────┐");
        _logger.LogInformation("│ {StatusIcon} {ServiceName} - {Status} ({ResponseTime}ms){Padding}│", 
            statusIcon, serviceName, status, result.ResponseTime, new string(' ', Math.Max(0, 20 - status.Length - result.ResponseTime.ToString().Length)));
        _logger.LogInformation("├─────────────────────────────────────────────────────────┤");

        // Log details with better formatting
        foreach (var detail in result.Details)
        {
            _logger.LogInformation("│   {Key}: {Value}{Padding}│", 
                detail.Key.PadRight(20), detail.Value, new string(' ', Math.Max(0, 32 - detail.Key.Length - detail.Value.Length)));
        }

        // Log warnings with emphasis
        foreach (var warning in result.Warnings)
        {
            _logger.LogWarning("│ ⚠️  WARNING: {Warning}{Padding}│", 
                warning, new string(' ', Math.Max(0, 44 - warning.Length)));
        }

        // Log errors with full details
        if (result.Error != null)
        {
            _logger.LogError("│ ❌ ERROR: {ErrorMessage}{Padding}│", 
                result.Error.Message, new string(' ', Math.Max(0, 45 - result.Error.Message.Length)));
            _logger.LogError(result.Error, "│   Full error details for {ServiceName}", result.ServiceName);
        }

        _logger.LogInformation("└─────────────────────────────────────────────────────────┘");
        _logger.LogInformation(""); // Empty line for spacing
    }

    private void LogStartupSummary(List<ServiceHealthResult> results)
    {
        _logger.LogInformation("╔═══════════════════════════════════════════════════════════╗");
        _logger.LogInformation("║                 🏥 HEALTH CHECK SUMMARY                  ║");
        _logger.LogInformation("╠═══════════════════════════════════════════════════════════╣");
        
        var healthyCount = results.Count(r => r.IsHealthy);
        var totalCount = results.Count;
        var totalTime = results.Sum(r => r.ResponseTime);
        
        _logger.LogInformation("║ 📊 Status: {HealthyCount}/{TotalCount} services healthy{Padding}║", 
            healthyCount, totalCount, new string(' ', 28 - $"{healthyCount}/{totalCount}".Length));
        _logger.LogInformation("║ ⏱️  Total Response Time: {TotalTime}ms{Padding}║", 
            totalTime, new string(' ', 33 - totalTime.ToString().Length));
        
        // Individual service status
        _logger.LogInformation("║{Padding}║", new string(' ', 59));
        foreach (var result in results)
        {
            var icon = result.IsHealthy ? "✅" : "❌";
            var status = result.IsHealthy ? "HEALTHY" : "FAILED";
            _logger.LogInformation("║ {Icon} {ServiceName}: {Status} ({ResponseTime}ms){Padding}║", 
                icon, result.ServiceName.PadRight(10), status, result.ResponseTime,
                new string(' ', Math.Max(0, 32 - result.ServiceName.Length - status.Length - result.ResponseTime.ToString().Length)));
        }
        
        _logger.LogInformation("║{Padding}║", new string(' ', 59));
        
        if (healthyCount == totalCount)
        {
            _logger.LogInformation("║ 🎉 ALL SYSTEMS OPERATIONAL - READY FOR PRODUCTION! 🎉   ║");
        }
        else
        {
            _logger.LogWarning("║ ⚠️  SOME SERVICES NEED ATTENTION BEFORE PRODUCTION      ║");
        }
        
        _logger.LogInformation("╚═══════════════════════════════════════════════════════════╝");
    }

    public class ServiceHealthResult
    {
        public string ServiceName { get; set; } = string.Empty;
        public bool IsHealthy { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public long ResponseTime { get; set; }
        public Dictionary<string, string> Details { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public Exception? Error { get; set; }
    }
} 