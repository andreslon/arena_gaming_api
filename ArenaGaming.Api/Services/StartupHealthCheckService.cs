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
        // Wait a bit for the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        _logger.LogInformation("🚀 Starting automatic health checks for all services...");
        
        var overallHealthy = true;
        var healthResults = new List<ServiceHealthResult>();

        // Test all services
        var postgresResult = await TestPostgreSQL();
        var redisResult = await TestRedis();
        var pulsarResult = await TestPulsar();

        healthResults.AddRange(new[] { postgresResult, redisResult, pulsarResult });

        // Log individual results
        foreach (var result in healthResults)
        {
            LogServiceResult(result);
            if (!result.IsHealthy)
                overallHealthy = false;
        }

        // Log overall result
        if (overallHealthy)
        {
            _logger.LogInformation("✅ ALL SERVICES ARE HEALTHY! Application is ready to handle requests.");
        }
        else
        {
            _logger.LogError("❌ SOME SERVICES ARE UNHEALTHY! Check the logs above for details.");
            
            // Optionally, you can decide to stop the application if critical services are down
            // _applicationLifetime.StopApplication();
        }

        // Log startup summary
        LogStartupSummary(healthResults);

        // Store results for API access
        _healthCheckResults.SetStartupResults(healthResults);
    }

    private async Task<ServiceHealthResult> TestPostgreSQL()
    {
        var result = new ServiceHealthResult { ServiceName = "PostgreSQL" };
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Test connection
            await dbContext.Database.OpenConnectionAsync();
            result.Details.Add("Connection", "✅ Successful");

            // Test migration status
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            if (!pendingMigrations.Any())
            {
                result.Details.Add("Migrations", "✅ All applied");
            }
            else
            {
                result.Details.Add("Migrations", $"⚠️ {pendingMigrations.Count()} pending");
                result.Warnings.Add($"Pending migrations: {string.Join(", ", pendingMigrations)}");
            }

            // Test basic query
            var gameCount = await dbContext.Games.CountAsync();
            var sessionCount = await dbContext.Sessions.CountAsync();
            result.Details.Add("Data Access", $"✅ Games: {gameCount}, Sessions: {sessionCount}");

            // Test a write operation (create and delete a test session)
            var testSession = new Session(Guid.NewGuid());
            dbContext.Sessions.Add(testSession);
            await dbContext.SaveChangesAsync();
            
            dbContext.Sessions.Remove(testSession);
            await dbContext.SaveChangesAsync();
            result.Details.Add("Write Operations", "✅ Create/Delete successful");

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
            using var scope = _serviceProvider.CreateScope();
            var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
            var database = redis.GetDatabase();

            // Test basic connectivity
            result.Details.Add("Connection", redis.IsConnected ? "✅ Connected" : "❌ Disconnected");

            // Test write/read operations
            var testKey = $"startup-test:{Guid.NewGuid()}";
            var testValue = $"test-value-{DateTime.UtcNow:yyyyMMddHHmmss}";
            
            await database.StringSetAsync(testKey, testValue, TimeSpan.FromSeconds(30));
            var retrievedValue = await database.StringGetAsync(testKey);
            await database.KeyDeleteAsync(testKey);

            if (retrievedValue == testValue)
            {
                result.Details.Add("Read/Write", "✅ Operations successful");
            }
            else
            {
                result.Details.Add("Read/Write", "❌ Data integrity issue");
                result.Warnings.Add("Redis read/write test failed - data mismatch");
            }

            // Get Redis info
            try
            {
                var server = redis.GetServer(redis.GetEndPoints().First());
                var info = await server.InfoAsync();
                var redisVersion = info.FirstOrDefault(i => i.Key == "redis_version")?.FirstOrDefault().Value ?? "Unknown";
                result.Details.Add("Server Info", $"✅ Version: {redisVersion}");
            }
            catch (Exception infoEx)
            {
                result.Details.Add("Server Info", "⚠️ Could not retrieve");
                result.Warnings.Add($"Could not get Redis server info: {infoEx.Message}");
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
            using var scope = _serviceProvider.CreateScope();
            var pulsarClient = scope.ServiceProvider.GetRequiredService<IPulsarClient>();

            var testTopic = $"startup-test-{Guid.NewGuid()}";
            
            // Test producer creation
            producer = pulsarClient.NewProducer()
                .Topic(testTopic)
                .Create();
            result.Details.Add("Producer", "✅ Created successfully");

            // Test consumer creation
            consumer = pulsarClient.NewConsumer()
                .Topic(testTopic)
                .SubscriptionName("startup-health-check")
                .SubscriptionType(SubscriptionType.Exclusive)
                .Create();
            result.Details.Add("Consumer", "✅ Created successfully");

            // Test message sending
            var testMessage = new
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Message = "Startup health check test message"
            };

            var messageBytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(testMessage));
            var messageId = await producer.Send(messageBytes);
            result.Details.Add("Message Send", $"✅ Message sent: {messageId}");

            // Test message receiving (with short timeout)
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
            
            if (!messageReceived)
            {
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
        var serviceName = result.ServiceName.PadRight(12);
        
        _logger.LogInformation("{StatusIcon} {ServiceName} | {Message} ({ResponseTime}ms)", 
            statusIcon, serviceName, result.Message, result.ResponseTime);

        // Log details
        foreach (var detail in result.Details)
        {
            _logger.LogInformation("   └─ {Key}: {Value}", detail.Key, detail.Value);
        }

        // Log warnings
        foreach (var warning in result.Warnings)
        {
            _logger.LogWarning("   ⚠️  {Warning}", warning);
        }

        // Log errors
        if (result.Error != null)
        {
            _logger.LogError(result.Error, "   ❌ Error details for {ServiceName}", result.ServiceName);
        }
    }

    private void LogStartupSummary(List<ServiceHealthResult> results)
    {
        _logger.LogInformation("═══════════════════════════════════════════════════════════");
        _logger.LogInformation("🏥 STARTUP HEALTH CHECK SUMMARY");
        _logger.LogInformation("═══════════════════════════════════════════════════════════");
        
        var healthyCount = results.Count(r => r.IsHealthy);
        var totalCount = results.Count;
        
        _logger.LogInformation("📊 Services Status: {HealthyCount}/{TotalCount} healthy", healthyCount, totalCount);
        _logger.LogInformation("⏱️  Total Response Time: {TotalTime}ms", results.Sum(r => r.ResponseTime));
        
        if (healthyCount == totalCount)
        {
            _logger.LogInformation("🎉 All systems operational - Application ready for production use!");
        }
        else
        {
            _logger.LogWarning("⚠️  Some services may need attention before production use.");
        }
        
        _logger.LogInformation("═══════════════════════════════════════════════════════════");
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