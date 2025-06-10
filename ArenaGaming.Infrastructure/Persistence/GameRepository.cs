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

public class GameRepository : IGameRepository
{
    private readonly IDatabase _database;
    private const string GAME_KEY_PREFIX = "game:";
    
    public GameRepository(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task<Game> AddAsync(Game game, CancellationToken cancellationToken = default)
    {
        var key = GAME_KEY_PREFIX + game.Id;
        var gameJson = JsonSerializer.Serialize(game);
        await _database.StringSetAsync(key, gameJson);
        
        // Set expiration to 24 hours
        await _database.KeyExpireAsync(key, TimeSpan.FromHours(24));
        
        return game;
    }

    public async Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var key = GAME_KEY_PREFIX + id;
        var gameJson = await _database.StringGetAsync(key);
        
        if (!gameJson.HasValue)
            return null;
            
        return JsonSerializer.Deserialize<Game>(gameJson!);
    }

    public async Task UpdateAsync(Game game, CancellationToken cancellationToken = default)
    {
        var key = GAME_KEY_PREFIX + game.Id;
        var gameJson = JsonSerializer.Serialize(game);
        await _database.StringSetAsync(key, gameJson);
    }

    public async Task<IEnumerable<Game>> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        // For simplicity, we'll iterate through all game keys and filter
        // In production, you might want to maintain a separate index
        var server = _database.Multiplexer.GetServer(_database.Multiplexer.GetEndPoints().First());
        var keys = server.Keys(pattern: GAME_KEY_PREFIX + "*");
        
        var games = new List<Game>();
        
        foreach (var key in keys)
        {
            var gameJson = await _database.StringGetAsync(key);
            if (gameJson.HasValue)
            {
                var game = JsonSerializer.Deserialize<Game>(gameJson!);
                if (game?.PlayerId == playerId)
                {
                    games.Add(game);
                }
            }
        }
        
        return games;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var key = GAME_KEY_PREFIX + id;
        await _database.KeyDeleteAsync(key);
    }
} 