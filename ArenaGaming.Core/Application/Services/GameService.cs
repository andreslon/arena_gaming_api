using System;
using System.Threading;
using System.Threading.Tasks;
using ArenaGaming.Core.Application.Interfaces;
using ArenaGaming.Core.Domain;
using ArenaGaming.Core.Domain.Events;

namespace ArenaGaming.Core.Application.Services;

public class GameService
{
    private readonly IGameRepository _gameRepository;
    private readonly IMoveRepository _moveRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICacheService _cacheService;

    public GameService(
        IGameRepository gameRepository,
        IMoveRepository moveRepository,
        IEventPublisher eventPublisher,
        ICacheService cacheService)
    {
        _gameRepository = gameRepository;
        _moveRepository = moveRepository;
        _eventPublisher = eventPublisher;
        _cacheService = cacheService;
    }

    public async Task<Game> CreateGameAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var game = new Game(playerId);
        await _gameRepository.AddAsync(game, cancellationToken);
        
        var gameStartedEvent = new GameStartedEvent(game.Id, playerId);
        await _eventPublisher.PublishAsync(gameStartedEvent, "game-started", cancellationToken);

        return game;
    }

    public async Task<Game> MakeMoveAsync(Guid gameId, Guid playerId, int position, CancellationToken cancellationToken = default)
    {
        // Get game from Redis - simple and fast!
        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);
        if (game == null)
            throw new ArgumentException("Game not found", nameof(gameId));

        // Debug: Log the game state
        var boardState = string.Join(",", game.Board.Select((c, i) => $"{i}:'{c}'"));
        Console.WriteLine($"[DEBUG] Game retrieved from Redis. Board: [{boardState}], Status: {game.Status}, CurrentSymbol: {game.CurrentPlayerSymbol}");

        // Make the move
        game.MakeMove(position, playerId);
        
        // Save back to Redis
        await _gameRepository.UpdateAsync(game, cancellationToken);

        // Cache the updated game state
        await _cacheService.SetAsync($"game:{gameId}", game, TimeSpan.FromMinutes(30), cancellationToken);

        // Publish move event
        var moveEvent = new MoveMadeEvent(gameId, playerId, position, game.Board);
        await _eventPublisher.PublishAsync(moveEvent, "move-made", cancellationToken);

        // If game ended, publish game ended event
        if (game.Status == GameStatus.Ended)
        {
            var gameEndedEvent = new GameEndedEvent(gameId, game.WinnerId, game.WinnerId == null);
            await _eventPublisher.PublishAsync(gameEndedEvent, "game-ended", cancellationToken);
        }

        return game;
    }



    public async Task<Game?> GetGameAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        // Try to get from cache first
        var cachedGame = await _cacheService.GetAsync<Game>($"game:{gameId}", cancellationToken);
        if (cachedGame != null)
            return cachedGame;

        // If not in cache, get from database
        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);
        if (game != null)
        {
            // Cache the game
            await _cacheService.SetAsync($"game:{gameId}", game, TimeSpan.FromMinutes(30), cancellationToken);
        }

        return game;
    }


} 