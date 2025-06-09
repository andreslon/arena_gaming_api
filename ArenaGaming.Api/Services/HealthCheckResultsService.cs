using static ArenaGaming.Api.Services.StartupHealthCheckService;

namespace ArenaGaming.Api.Services;

public interface IHealthCheckResultsService
{
    void SetStartupResults(List<ServiceHealthResult> results);
    List<ServiceHealthResult> GetStartupResults();
    DateTime? GetLastCheckTime();
    bool AreAllServicesHealthy();
}

public class HealthCheckResultsService : IHealthCheckResultsService
{
    private List<ServiceHealthResult> _lastResults = new();
    private DateTime? _lastCheckTime;
    private readonly object _lock = new();

    public void SetStartupResults(List<ServiceHealthResult> results)
    {
        lock (_lock)
        {
            _lastResults = results.Select(r => new ServiceHealthResult
            {
                ServiceName = r.ServiceName,
                IsHealthy = r.IsHealthy,
                Message = r.Message,
                ResponseTime = r.ResponseTime,
                Details = new Dictionary<string, string>(r.Details),
                Warnings = new List<string>(r.Warnings),
                Error = r.Error
            }).ToList();
            
            _lastCheckTime = DateTime.UtcNow;
        }
    }

    public List<ServiceHealthResult> GetStartupResults()
    {
        lock (_lock)
        {
            return _lastResults.Select(r => new ServiceHealthResult
            {
                ServiceName = r.ServiceName,
                IsHealthy = r.IsHealthy,
                Message = r.Message,
                ResponseTime = r.ResponseTime,
                Details = new Dictionary<string, string>(r.Details),
                Warnings = new List<string>(r.Warnings),
                Error = r.Error
            }).ToList();
        }
    }

    public DateTime? GetLastCheckTime()
    {
        lock (_lock)
        {
            return _lastCheckTime;
        }
    }

    public bool AreAllServicesHealthy()
    {
        lock (_lock)
        {
            return _lastResults.Any() && _lastResults.All(r => r.IsHealthy);
        }
    }
} 