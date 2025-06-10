using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ArenaGaming.Core.Application.Interfaces;
using ArenaGaming.Core.Domain;
using StackExchange.Redis;

namespace ArenaGaming.Infrastructure.Persistence;

public class SessionRepository : ISessionRepository
{
    private readonly IDatabase _database;
    private const string SESSION_KEY_PREFIX = "session:";
    
    public SessionRepository(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task<Session> AddAsync(Session session, CancellationToken cancellationToken = default)
    {
        var key = SESSION_KEY_PREFIX + session.Id;
        var sessionJson = JsonSerializer.Serialize(session);
        await _database.StringSetAsync(key, sessionJson);
        
        // Set expiration to 24 hours
        await _database.KeyExpireAsync(key, TimeSpan.FromHours(24));
        
        return session;
    }

    public async Task<Session?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var key = SESSION_KEY_PREFIX + id;
        var sessionJson = await _database.StringGetAsync(key);
        
        if (!sessionJson.HasValue)
            return null;
            
        return JsonSerializer.Deserialize<Session>(sessionJson!);
    }

    public async Task UpdateAsync(Session session, CancellationToken cancellationToken = default)
    {
        var key = SESSION_KEY_PREFIX + session.Id;
        var sessionJson = JsonSerializer.Serialize(session);
        await _database.StringSetAsync(key, sessionJson);
    }

    public async Task<IEnumerable<Session>> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var server = _database.Multiplexer.GetServer(_database.Multiplexer.GetEndPoints().First());
        var keys = server.Keys(pattern: SESSION_KEY_PREFIX + "*");
        
        var sessions = new List<Session>();
        
        foreach (var key in keys)
        {
            var sessionJson = await _database.StringGetAsync(key);
            if (sessionJson.HasValue)
            {
                var session = JsonSerializer.Deserialize<Session>(sessionJson!);
                if (session?.PlayerId == playerId)
                {
                    sessions.Add(session);
                }
            }
        }
        
        return sessions;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var key = SESSION_KEY_PREFIX + id;
        await _database.KeyDeleteAsync(key);
    }
} 