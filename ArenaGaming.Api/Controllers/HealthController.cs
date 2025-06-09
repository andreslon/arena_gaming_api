using ArenaGaming.Core.Domain;
using ArenaGaming.Infrastructure.Persistence;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Buffers;
using System.Diagnostics;
using System.Text.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ArenaGaming.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
            private readonly ApplicationDbContext _dbContext;
    private readonly IConnectionMultiplexer _redis;
    private readonly IPulsarClient _pulsarClient;
    private readonly ILogger<HealthController> _logger;
    private readonly ArenaGaming.Api.Services.IHealthCheckResultsService _healthCheckResults;

    public HealthController(
        ApplicationDbContext dbContext,
        IConnectionMultiplexer redis,
        IPulsarClient pulsarClient,
        ILogger<HealthController> logger,
        ArenaGaming.Api.Services.IHealthCheckResultsService healthCheckResults)
    {
        _dbContext = dbContext;
        _redis = redis;
        _pulsarClient = pulsarClient;
        _logger = logger;
        _healthCheckResults = healthCheckResults;
    }

        /// <summary>
        /// Check overall health status
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetHealth()
        {
            var healthStatus = new HealthStatusResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = "Healthy"
            };

            var tasks = new List<Task<ServiceHealthCheck>>
            {
                CheckPostgreSqlHealth(),
                CheckRedisHealth(),
                CheckPulsarHealth()
            };

            var results = await Task.WhenAll(tasks);
            healthStatus.Services = results.ToList();

            // Determine overall status
            if (results.Any(r => r.Status == "Unhealthy"))
            {
                healthStatus.Status = "Unhealthy";
                return StatusCode(503, healthStatus);
            }
            else if (results.Any(r => r.Status == "Degraded"))
            {
                healthStatus.Status = "Degraded";
            }

            return Ok(healthStatus);
        }

        /// <summary>
        /// Check PostgreSQL database health
        /// </summary>
        [HttpGet("postgresql")]
        public async Task<IActionResult> GetPostgreSqlHealth()
        {
            var result = await CheckPostgreSqlHealth();
            return result.Status == "Healthy" ? Ok(result) : StatusCode(503, result);
        }

        /// <summary>
        /// Check Redis cache health
        /// </summary>
        [HttpGet("redis")]
        public async Task<IActionResult> GetRedisHealth()
        {
            var result = await CheckRedisHealth();
            return result.Status == "Healthy" ? Ok(result) : StatusCode(503, result);
        }

            /// <summary>
    /// Check Pulsar messaging health
    /// </summary>
    [HttpGet("pulsar")]
    public async Task<IActionResult> GetPulsarHealth()
    {
        var result = await CheckPulsarHealth();
        return result.Status == "Healthy" ? Ok(result) : StatusCode(503, result);
    }

    /// <summary>
    /// Get startup health check results
    /// </summary>
    [HttpGet("startup")]
    public IActionResult GetStartupHealthResults()
    {
        var results = _healthCheckResults.GetStartupResults();
        var lastCheckTime = _healthCheckResults.GetLastCheckTime();
        var allHealthy = _healthCheckResults.AreAllServicesHealthy();

        if (lastCheckTime == null)
        {
            return Ok(new
            {
                Status = "Pending",
                Message = "Startup health checks have not completed yet",
                LastCheckTime = (DateTime?)null,
                Services = new List<object>()
            });
        }

        return Ok(new
        {
            Status = allHealthy ? "Healthy" : "Unhealthy",
            Message = allHealthy ? "All startup checks passed" : "Some startup checks failed",
            LastCheckTime = lastCheckTime,
            Services = results.Select(r => new
            {
                r.ServiceName,
                r.IsHealthy,
                r.Message,
                r.ResponseTime,
                r.Details,
                r.Warnings,
                HasError = r.Error != null,
                ErrorMessage = r.Error?.Message
            })
        });
    }

        private async Task<ServiceHealthCheck> CheckPostgreSqlHealth()
        {
            var stopwatch = Stopwatch.StartNew();
            var healthCheck = new ServiceHealthCheck
            {
                Service = "PostgreSQL",
                CheckTime = DateTime.UtcNow
            };

            try
            {
                // Test database connection
                await _dbContext.Database.OpenConnectionAsync();
                await _dbContext.Database.CloseConnectionAsync();

                // Test a simple query
                var gameCount = await _dbContext.Games.CountAsync();
                
                stopwatch.Stop();
                healthCheck.Status = "Healthy";
                healthCheck.ResponseTime = stopwatch.ElapsedMilliseconds;
                healthCheck.Message = $"Database connection successful. Games count: {gameCount}";
                healthCheck.Details = new Dictionary<string, object>
                {
                    ["ConnectionState"] = _dbContext.Database.GetConnectionString() ?? "Not available",
                    ["GamesCount"] = gameCount,
                    ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                healthCheck.Status = "Unhealthy";
                healthCheck.ResponseTime = stopwatch.ElapsedMilliseconds;
                healthCheck.Message = $"Database connection failed: {ex.Message}";
                healthCheck.Details = new Dictionary<string, object>
                {
                    ["Error"] = ex.Message,
                    ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds
                };
                
                _logger.LogError(ex, "PostgreSQL health check failed");
            }

            return healthCheck;
        }

        private async Task<ServiceHealthCheck> CheckRedisHealth()
        {
            var stopwatch = Stopwatch.StartNew();
            var healthCheck = new ServiceHealthCheck
            {
                Service = "Redis",
                CheckTime = DateTime.UtcNow
            };

            try
            {
                var database = _redis.GetDatabase();
                var testKey = $"health-check:{Guid.NewGuid()}";
                var testValue = DateTime.UtcNow.ToString();

                // Test Redis write
                await database.StringSetAsync(testKey, testValue, TimeSpan.FromSeconds(10));
                
                // Test Redis read
                var retrievedValue = await database.StringGetAsync(testKey);
                
                // Cleanup
                await database.KeyDeleteAsync(testKey);

                // Get Redis info
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var info = await server.InfoAsync();
                
                stopwatch.Stop();
                
                if (retrievedValue == testValue)
                {
                    healthCheck.Status = "Healthy";
                    healthCheck.Message = "Redis connection and operations successful";
                }
                else
                {
                    healthCheck.Status = "Degraded";
                    healthCheck.Message = "Redis connection successful but data integrity issue";
                }

                healthCheck.ResponseTime = stopwatch.ElapsedMilliseconds;
                healthCheck.Details = new Dictionary<string, object>
                {
                    ["IsConnected"] = _redis.IsConnected,
                    ["EndPoints"] = _redis.GetEndPoints().Select(ep => ep.ToString()).ToArray(),
                    ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds,
                    ["RedisVersion"] = info.FirstOrDefault(i => i.Key == "redis_version")?.FirstOrDefault().Value ?? "Unknown"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                healthCheck.Status = "Unhealthy";
                healthCheck.ResponseTime = stopwatch.ElapsedMilliseconds;
                healthCheck.Message = $"Redis connection failed: {ex.Message}";
                healthCheck.Details = new Dictionary<string, object>
                {
                    ["Error"] = ex.Message,
                    ["IsConnected"] = _redis.IsConnected,
                    ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds
                };
                
                _logger.LogError(ex, "Redis health check failed");
            }

            return healthCheck;
        }

        private async Task<ServiceHealthCheck> CheckPulsarHealth()
        {
            var stopwatch = Stopwatch.StartNew();
            var healthCheck = new ServiceHealthCheck
            {
                Service = "Pulsar",
                CheckTime = DateTime.UtcNow
            };

            IProducer<ReadOnlySequence<byte>>? producer = null;
            IConsumer<ReadOnlySequence<byte>>? consumer = null;

            try
            {
                var testTopic = "health-check-topic";
                var testMessage = new
                {
                    Id = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow,
                    Message = "Health check test message"
                };

                // Create producer
                producer = _pulsarClient.NewProducer()
                    .Topic(testTopic)
                    .Create();

                // Create consumer
                consumer = _pulsarClient.NewConsumer()
                    .Topic(testTopic)
                    .SubscriptionName("health-check-subscription")
                    .SubscriptionType(SubscriptionType.Exclusive)
                    .Create();

                // Send test message
                var messageBytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(testMessage));
                var messageId = await producer.Send(messageBytes);

                // Try to receive the message (with timeout)
                using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var receivedMessage = false;

                try
                {
                    await foreach (var message in consumer.Messages(cancellationTokenSource.Token))
                    {
                        await consumer.Acknowledge(message);
                        receivedMessage = true;
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    // Timeout is expected in this test
                }

                stopwatch.Stop();
                
                healthCheck.Status = receivedMessage ? "Healthy" : "Degraded";
                healthCheck.ResponseTime = stopwatch.ElapsedMilliseconds;
                healthCheck.Message = receivedMessage 
                    ? "Pulsar producer and consumer working correctly" 
                    : "Pulsar producer working, consumer test timed out (may be normal)";
                    
                healthCheck.Details = new Dictionary<string, object>
                {
                    ["ProducerWorking"] = true,
                    ["ConsumerWorking"] = receivedMessage,
                    ["MessageId"] = messageId.ToString(),
                    ["TestTopic"] = testTopic,
                    ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                healthCheck.Status = "Unhealthy";
                healthCheck.ResponseTime = stopwatch.ElapsedMilliseconds;
                healthCheck.Message = $"Pulsar connection failed: {ex.Message}";
                healthCheck.Details = new Dictionary<string, object>
                {
                    ["Error"] = ex.Message,
                    ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds
                };
                
                _logger.LogError(ex, "Pulsar health check failed");
            }
            finally
            {
                // Cleanup resources
                if (producer != null)
                    await producer.DisposeAsync();
                if (consumer != null)
                    await consumer.DisposeAsync();
            }

            return healthCheck;
        }

        /// <summary>
        /// Test PostgreSQL with sample data operations
        /// </summary>
        [HttpPost("postgresql/test")]
        public async Task<IActionResult> TestPostgreSqlOperations()
        {
            try
            {
                var testResults = new Dictionary<string, object>();
                var stopwatch = Stopwatch.StartNew();

                // Test database connection
                await _dbContext.Database.OpenConnectionAsync();
                testResults["ConnectionTest"] = "Success";

                // Test creating a test session
                var testSession = new Session(Guid.NewGuid());

                _dbContext.Sessions.Add(testSession);
                await _dbContext.SaveChangesAsync();
                testResults["CreateOperation"] = "Success";

                // Test reading the session
                var retrievedSession = await _dbContext.Sessions.FindAsync(testSession.Id);
                testResults["ReadOperation"] = retrievedSession != null ? "Success" : "Failed";

                // Test updating the session (end it)
                if (retrievedSession != null)
                {
                    retrievedSession.End();
                    await _dbContext.SaveChangesAsync();
                    testResults["UpdateOperation"] = "Success";
                }

                // Test deleting the session
                if (retrievedSession != null)
                {
                    _dbContext.Sessions.Remove(retrievedSession);
                    await _dbContext.SaveChangesAsync();
                    testResults["DeleteOperation"] = "Success";
                }

                await _dbContext.Database.CloseConnectionAsync();
                stopwatch.Stop();

                return Ok(new
                {
                    Service = "PostgreSQL",
                    Status = "All operations successful",
                    ResponseTime = stopwatch.ElapsedMilliseconds,
                    TestResults = testResults
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostgreSQL test operations failed");
                return StatusCode(500, new
                {
                    Service = "PostgreSQL",
                    Status = "Test failed",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Test Redis with various data types
        /// </summary>
        [HttpPost("redis/test")]
        public async Task<IActionResult> TestRedisOperations()
        {
            try
            {
                var testResults = new Dictionary<string, object>();
                var stopwatch = Stopwatch.StartNew();
                var database = _redis.GetDatabase();

                // Test String operations
                var stringKey = $"test:string:{Guid.NewGuid()}";
                var stringValue = "Hello Redis!";
                await database.StringSetAsync(stringKey, stringValue, TimeSpan.FromMinutes(1));
                var retrievedString = await database.StringGetAsync(stringKey);
                testResults["StringOperations"] = retrievedString == stringValue ? "Success" : "Failed";

                // Test Hash operations
                var hashKey = $"test:hash:{Guid.NewGuid()}";
                await database.HashSetAsync(hashKey, new HashEntry[]
                {
                    new("field1", "value1"),
                    new("field2", "value2")
                });
                await database.KeyExpireAsync(hashKey, TimeSpan.FromMinutes(1));
                var hashValue = await database.HashGetAsync(hashKey, "field1");
                testResults["HashOperations"] = hashValue == "value1" ? "Success" : "Failed";

                // Test List operations
                var listKey = $"test:list:{Guid.NewGuid()}";
                await database.ListLeftPushAsync(listKey, "item1");
                await database.ListLeftPushAsync(listKey, "item2");
                await database.KeyExpireAsync(listKey, TimeSpan.FromMinutes(1));
                var listLength = await database.ListLengthAsync(listKey);
                testResults["ListOperations"] = listLength == 2 ? "Success" : "Failed";

                // Test Set operations
                var setKey = $"test:set:{Guid.NewGuid()}";
                await database.SetAddAsync(setKey, "member1");
                await database.SetAddAsync(setKey, "member2");
                await database.KeyExpireAsync(setKey, TimeSpan.FromMinutes(1));
                var setMembers = await database.SetMembersAsync(setKey);
                testResults["SetOperations"] = setMembers.Length == 2 ? "Success" : "Failed";

                // Cleanup
                await database.KeyDeleteAsync(new RedisKey[] { stringKey, hashKey, listKey, setKey });

                stopwatch.Stop();

                return Ok(new
                {
                    Service = "Redis",
                    Status = "All operations successful",
                    ResponseTime = stopwatch.ElapsedMilliseconds,
                    TestResults = testResults
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis test operations failed");
                return StatusCode(500, new
                {
                    Service = "Redis",
                    Status = "Test failed",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Test Pulsar with multiple topics and message types
        /// </summary>
        [HttpPost("pulsar/test")]
        public async Task<IActionResult> TestPulsarOperations()
        {
                    var producers = new List<IProducer<ReadOnlySequence<byte>>>();
        var consumers = new List<IConsumer<ReadOnlySequence<byte>>>();

            try
            {
                var testResults = new Dictionary<string, object>();
                var stopwatch = Stopwatch.StartNew();

                // Test multiple topics
                var topics = new[] { "test-topic-1", "test-topic-2", "test-topic-3" };
                var messagesReceived = 0;
                var messagesSent = 0;

                foreach (var topic in topics)
                {
                    // Create producer
                    var producer = _pulsarClient.NewProducer()
                        .Topic(topic)
                        .Create();
                    producers.Add(producer);

                    // Create consumer
                    var consumer = _pulsarClient.NewConsumer()
                        .Topic(topic)
                        .SubscriptionName($"test-subscription-{topic}")
                        .SubscriptionType(SubscriptionType.Exclusive)
                        .Create();
                    consumers.Add(consumer);

                    // Send test messages
                    for (int i = 0; i < 3; i++)
                    {
                        var testMessage = new
                        {
                            Id = Guid.NewGuid(),
                            Topic = topic,
                            MessageNumber = i + 1,
                            Timestamp = DateTime.UtcNow,
                            Content = $"Test message {i + 1} for {topic}"
                        };

                        var messageBytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(testMessage));
                        await producer.Send(messageBytes);
                        messagesSent++;
                    }
                }

                // Try to receive messages
                using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                var receiveTasks = consumers.Select(async consumer =>
                {
                    var messagesForThisConsumer = 0;
                    try
                    {
                        await foreach (var message in consumer.Messages(cancellationTokenSource.Token))
                        {
                            await consumer.Acknowledge(message);
                            messagesForThisConsumer++;
                            Interlocked.Increment(ref messagesReceived);
                            
                            if (messagesForThisConsumer >= 3) // Expected 3 messages per topic
                                break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when timeout occurs
                    }
                    return messagesForThisConsumer;
                });

                var receivedCounts = await Task.WhenAll(receiveTasks);

                stopwatch.Stop();

                testResults["TopicsTested"] = topics.Length;
                testResults["MessagesSent"] = messagesSent;
                testResults["MessagesReceived"] = messagesReceived;
                testResults["ProducerSuccess"] = messagesSent > 0;
                testResults["ConsumerSuccess"] = messagesReceived > 0;
                testResults["TopicResults"] = topics.Zip(receivedCounts, (topic, count) => new { Topic = topic, MessagesReceived = count });

                var status = messagesReceived > 0 ? "Success" : "Partial - Messages sent but receiving timed out";

                return Ok(new
                {
                    Service = "Pulsar",
                    Status = status,
                    ResponseTime = stopwatch.ElapsedMilliseconds,
                    TestResults = testResults
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pulsar test operations failed");
                return StatusCode(500, new
                {
                    Service = "Pulsar",
                    Status = "Test failed",
                    Error = ex.Message
                });
            }
            finally
            {
                // Cleanup all resources
                foreach (var producer in producers)
                {
                    await producer.DisposeAsync();
                }
                foreach (var consumer in consumers)
                {
                    await consumer.DisposeAsync();
                }
            }
        }
    }

    public class HealthStatusResponse
    {
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<ServiceHealthCheck> Services { get; set; } = new();
    }

    public class ServiceHealthCheck
    {
        public string Service { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CheckTime { get; set; }
        public long ResponseTime { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> Details { get; set; } = new();
    }
}
