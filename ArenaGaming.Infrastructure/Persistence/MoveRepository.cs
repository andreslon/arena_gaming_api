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

public class MoveRepository : IMoveRepository
{
    private readonly IDatabase _database;
    private const string MOVE_KEY_PREFIX = "move:";
    private const string GAME_MOVES_KEY_PREFIX = "game_moves:";
    
    public MoveRepository(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task<Move> AddAsync(Move move, CancellationToken cancellationToken = default)
    {
        var key = MOVE_KEY_PREFIX + move.Id;
        var moveJson = JsonSerializer.Serialize(move);
        await _database.StringSetAsync(key, moveJson);
        
        // Also add to game moves list for easy retrieval
        var gameMovesKey = GAME_MOVES_KEY_PREFIX + move.GameId;
        await _database.ListRightPushAsync(gameMovesKey, move.Id.ToString());
        
        // Set expiration to 24 hours
        await _database.KeyExpireAsync(key, TimeSpan.FromHours(24));
        await _database.KeyExpireAsync(gameMovesKey, TimeSpan.FromHours(24));
        
        return move;
    }

    public async Task<Move?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var key = MOVE_KEY_PREFIX + id;
        var moveJson = await _database.StringGetAsync(key);
        
        if (!moveJson.HasValue)
            return null;
            
        return JsonSerializer.Deserialize<Move>(moveJson!);
    }

    public async Task<IEnumerable<Move>> GetByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var gameMovesKey = GAME_MOVES_KEY_PREFIX + gameId;
        var moveIds = await _database.ListRangeAsync(gameMovesKey);
        
        var moves = new List<Move>();
        
        foreach (var moveId in moveIds)
        {
            if (Guid.TryParse(moveId, out var id))
            {
                var move = await GetByIdAsync(id, cancellationToken);
                if (move != null)
                {
                    moves.Add(move);
                }
            }
        }
        
        return moves.OrderBy(m => m.Timestamp);
    }

    public async Task UpdateAsync(Move move, CancellationToken cancellationToken = default)
    {
        var key = MOVE_KEY_PREFIX + move.Id;
        var moveJson = JsonSerializer.Serialize(move);
        await _database.StringSetAsync(key, moveJson);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var key = MOVE_KEY_PREFIX + id;
        await _database.KeyDeleteAsync(key);
    }
} 