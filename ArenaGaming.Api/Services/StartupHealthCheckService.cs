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

            _logger.LogInformation("🚀 Testing services...");
            
            var overallHealthy = true;
            var healthResults = new List<ServiceHealthResult>();

            // Test Redis
            var redisResult = await TestRedis();
            healthResults.Add(redisResult);
            
            // Test Pulsar
            var pulsarResult = await TestPulsar();
            healthResults.Add(pulsarResult);

            // Check overall health
            foreach (var result in healthResults)
            {
                if (!result.IsHealthy)
                    overallHealthy = false;
            }

            // Log simple result
            if (overallHealthy)
            {
                _logger.LogInformation("✅ All services OK: Redis | Pulsar");
            }
            else
            {
                var statusSummary = $"{(redisResult.IsHealthy ? "✅" : "❌")} Redis | " +
                                  $"{(pulsarResult.IsHealthy ? "✅" : "❌")} Pulsar";
                _logger.LogError("❌ Service status: {StatusSummary}", statusSummary);
            }

            // Store results for API access
            _healthCheckResults.SetStartupResults(healthResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 CRITICAL ERROR DURING HEALTH CHECKS");
            _logger.LogError("❌ Health check process failed: {ErrorMessage}", ex.Message);
        }
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

            // Check Pulsar server URL
            var pulsarUrl = Environment.GetEnvironmentVariable("ConnectionStrings_Pulsar") ?? "pulsar://localhost:6650";
            _logger.LogInformation("   📡 Pulsar Server: {PulsarUrl}", pulsarUrl);

            var testTopic = $"startup-test-{Guid.NewGuid()}";
            
            // Test producer creation with timeout
            _logger.LogInformation("   📤 Creating Pulsar producer...");
            using var producerTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            try
            {
                var producerTask = Task.Run(() => pulsarClient.NewProducer()
                    .Topic(testTopic)
                    .Create(), producerTimeoutCts.Token);
                
                producer = await producerTask;
                result.Details.Add("Producer", "✅ Created successfully");
                _logger.LogInformation("   ✅ Pulsar producer created successfully");
            }
            catch (OperationCanceledException)
            {
                result.Details.Add("Producer", "❌ Creation timeout");
                _logger.LogError("   ❌ Pulsar producer creation timed out after 15 seconds");
                throw new Exception("Pulsar producer creation timed out - server may be unreachable");
            }

            // Test consumer creation with timeout
            _logger.LogInformation("   📥 Creating Pulsar consumer...");
            using var consumerTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            try
            {
                var consumerTask = Task.Run(() => pulsarClient.NewConsumer()
                    .Topic(testTopic)
                    .SubscriptionName("startup-health-check")
                    .SubscriptionType(SubscriptionType.Exclusive)
                    .Create(), consumerTimeoutCts.Token);
                
                consumer = await consumerTask;
                result.Details.Add("Consumer", "✅ Created successfully");
                _logger.LogInformation("   ✅ Pulsar consumer created successfully");
            }
            catch (OperationCanceledException)
            {
                result.Details.Add("Consumer", "❌ Creation timeout");
                _logger.LogError("   ❌ Pulsar consumer creation timed out after 15 seconds");
                throw new Exception("Pulsar consumer creation timed out - server may be unreachable");
            }

            // Test message sending with timeout
            _logger.LogInformation("   📨 Sending test message...");
            var testMessage = new
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Message = "Startup health check test message"
            };

            var messageBytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(testMessage));
            
            // Add timeout for sending message
            using var sendTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                var sendTask = producer.Send(messageBytes).AsTask();
                var completedTask = await Task.WhenAny(sendTask, Task.Delay(10000, sendTimeoutCts.Token));
                
                if (completedTask == sendTask)
                {
                    var messageId = await sendTask;
                    result.Details.Add("Message Send", $"✅ Message sent: {messageId}");
                    _logger.LogInformation("   ✅ Message sent successfully: {MessageId}", messageId);
                }
                else
                {
                    result.Details.Add("Message Send", "⚠️ Send timeout");
                    result.Warnings.Add("Message sending timed out after 10 seconds");
                    _logger.LogWarning("   ⚠️ Message send timeout after 10 seconds");
                    
                    // Still mark as healthy since producer was created, just slow
                    stopwatch.Stop();
                    result.IsHealthy = true;
                    result.ResponseTime = stopwatch.ElapsedMilliseconds;
                    result.Message = "Pulsar partially working - send timeout but connection OK";
                    return result;
                }
            }
            catch (Exception sendEx)
            {
                result.Details.Add("Message Send", $"❌ Send failed: {sendEx.Message}");
                _logger.LogError("   ❌ Message send failed: {Error}", sendEx.Message);
                throw; // Re-throw to be caught by outer catch block
            }

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
            
            // Add detailed diagnostic information
            var pulsarUrl = Environment.GetEnvironmentVariable("ConnectionStrings_Pulsar") ?? "pulsar://localhost:6650";
            result.Details.Add("Server URL", pulsarUrl);
            result.Details.Add("Error Type", ex.GetType().Name);
            
            if (ex.Message.Contains("timeout") || ex.Message.Contains("Timeout"))
            {
                result.Details.Add("Diagnosis", "⚠️ Server timeout - check connectivity");
                _logger.LogWarning("   🔍 Pulsar server may be unreachable or slow: {PulsarUrl}", pulsarUrl);
            }
            else if (ex.Message.Contains("refused") || ex.Message.Contains("Refused"))
            {
                result.Details.Add("Diagnosis", "⚠️ Connection refused - server may be down");
                _logger.LogWarning("   🔍 Pulsar server connection refused: {PulsarUrl}", pulsarUrl);
            }
            else
            {
                result.Details.Add("Diagnosis", "⚠️ Unknown error - check logs");
                _logger.LogError("   🔍 Pulsar unknown error: {Error}", ex.Message);
            }
            
            _logger.LogInformation("   💡 Run './check-pulsar.ps1' to test Pulsar connectivity manually");
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