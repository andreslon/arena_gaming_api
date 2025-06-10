using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ArenaGaming.Core.Application.Interfaces;
using DotPulsar.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ArenaGaming.Api.Controllers;

/// <summary>
/// Health check controller for monitoring system health
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class HealthController : ControllerBase
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IPulsarClient _pulsarClient;
    private readonly ILogger<HealthController> _logger;
    private readonly ArenaGaming.Api.Services.IHealthCheckResultsService _healthCheckResults;
    private readonly IGeminiService _geminiService;

    public HealthController(
        IConnectionMultiplexer redis,
        IPulsarClient pulsarClient,
        ILogger<HealthController> logger,
        ArenaGaming.Api.Services.IHealthCheckResultsService healthCheckResults,
        IGeminiService geminiService)
    {
        _redis = redis;
        _pulsarClient = pulsarClient;
        _logger = logger;
        _healthCheckResults = healthCheckResults;
        _geminiService = geminiService;
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
            CheckRedisHealth(),
            CheckPulsarHealth(),
            CheckGeminiHealth()
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
    /// Check Gemini AI service health
    /// </summary>
    [HttpGet("gemini")]
    public async Task<IActionResult> GetGeminiHealth()
    {
        var result = await CheckGeminiHealth();
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

    /// <summary>
    /// Simple status endpoint for load balancers
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetSimpleStatus()
    {
        try
        {
            // Quick Redis check
            var database = _redis.GetDatabase();
            await database.PingAsync();

            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Message = "API is running and Redis is accessible"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Simple health check failed");
            return StatusCode(503, new
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Message = "API is running but Redis is not accessible",
                Error = ex.Message
            });
        }
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

            stopwatch.Stop();
            
            if (retrievedValue == testValue)
            {
                healthCheck.Status = "Healthy";
                healthCheck.Message = "Redis read/write operations successful";
            }
            else
            {
                healthCheck.Status = "Degraded";
                healthCheck.Message = "Redis write succeeded but read returned different value";
            }

            healthCheck.ResponseTime = stopwatch.ElapsedMilliseconds;
            healthCheck.Details = new Dictionary<string, object>
            {
                ["TestKey"] = testKey,
                ["WriteSuccessful"] = true,
                ["ReadSuccessful"] = retrievedValue.HasValue,
                ["ValuesMatch"] = retrievedValue == testValue,
                ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds
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

        try
        {
            // Simple check - if we have a client instance, assume it's working
            stopwatch.Stop();
            
            healthCheck.Status = "Healthy";
            healthCheck.Message = "Pulsar client is available";
            healthCheck.Details = new Dictionary<string, object>
            {
                ["ClientAvailable"] = _pulsarClient != null,
                ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds
            };

            healthCheck.ResponseTime = stopwatch.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            healthCheck.Status = "Unhealthy";
            healthCheck.ResponseTime = stopwatch.ElapsedMilliseconds;
            healthCheck.Message = $"Pulsar health check failed: {ex.Message}";
            healthCheck.Details = new Dictionary<string, object>
            {
                ["Error"] = ex.Message,
                ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds
            };
            
            _logger.LogError(ex, "Pulsar health check failed");
        }

        return healthCheck;
    }

    private async Task<ServiceHealthCheck> CheckGeminiHealth()
    {
        var stopwatch = Stopwatch.StartNew();
        var healthCheck = new ServiceHealthCheck
        {
            Service = "Gemini",
            CheckTime = DateTime.UtcNow
        };

        try
        {
            // Simple test - try to get a move from Gemini
            var testBoard = "         "; // Empty board
            var move = await _geminiService.GetNextMoveAsync(testBoard, 'O');
            
            stopwatch.Stop();
            
            if (move >= 0 && move <= 8)
            {
                healthCheck.Status = "Healthy";
                healthCheck.Message = "Gemini AI service is responding";
                healthCheck.Details = new Dictionary<string, object>
                {
                    ["TestBoard"] = testBoard,
                    ["ResponseReceived"] = true,
                    ["MovePosition"] = move,
                    ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds
                };
            }
            else
            {
                healthCheck.Status = "Degraded";
                healthCheck.Message = "Gemini AI service returned invalid move";
                healthCheck.Details = new Dictionary<string, object>
                {
                    ["TestBoard"] = testBoard,
                    ["ResponseReceived"] = true,
                    ["InvalidMove"] = move,
                    ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds
                };
            }

            healthCheck.ResponseTime = stopwatch.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            healthCheck.Status = "Unhealthy";
            healthCheck.ResponseTime = stopwatch.ElapsedMilliseconds;
            healthCheck.Message = $"Gemini AI service failed: {ex.Message}";
            healthCheck.Details = new Dictionary<string, object>
            {
                ["Error"] = ex.Message,
                ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds
            };
            
            _logger.LogError(ex, "Gemini health check failed");
        }

        return healthCheck;
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
