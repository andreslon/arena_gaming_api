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
        const int maxRetries = 3;
        var retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                // Get fresh game directly from database (bypass cache) to avoid concurrency issues
                var game = await GetFreshGameFromDatabaseAsync(gameId, cancellationToken);
                if (game == null)
                    throw new ArgumentException("Game not found", nameof(gameId));

                // Debug: Log the game state
                var boardState = string.Join(",", game.Board.Select((c, i) => $"{i}:'{c}'"));
                Console.WriteLine($"[DEBUG] Game retrieved from DB. Board: [{boardState}], Status: {game.Status}, CurrentSymbol: {game.CurrentPlayerSymbol}");

                // Validate move before attempting to save
                ValidateMove(game, position, playerId);

                game.MakeMove(position, playerId);
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
            catch (Exception ex) when (IsConcurrencyException(ex))
            {
                retryCount++;
                if (retryCount >= maxRetries)
                {
                    throw new InvalidOperationException(
                        "Unable to complete the move due to concurrent modifications. Please try again.");
                }

                // Wait a short time before retrying to reduce contention
                await Task.Delay(TimeSpan.FromMilliseconds(100 * retryCount), cancellationToken);
            }
        }

        // This line should never be reached due to the throw above, but is here for completeness
        throw new InvalidOperationException("Unexpected error in MakeMoveAsync");
    }

    private static bool IsConcurrencyException(Exception ex)
    {
        // Check for common concurrency exception patterns
        return ex.GetType().Name.Contains("ConcurrencyException") ||
               ex.Message.Contains("expected to affect 1 row") ||
               ex.Message.Contains("concurrency");
    }

    private void ValidateMove(Game game, int position, Guid playerId)
    {
        if (game.Status != GameStatus.InProgress)
            throw new InvalidOperationException("Game is not in progress");

        if (position < 0 || position >= 9)
            throw new ArgumentException("Invalid position", nameof(position));

        if (game.Board[position] != ' ')
            throw new InvalidOperationException("Position is already taken");
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

    /// <summary>
    /// Gets fresh game data directly from database, bypassing cache.
    /// Use this method when you need to update the game to avoid concurrency issues.
    /// </summary>
    private async Task<Game?> GetFreshGameFromDatabaseAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        return await _gameRepository.GetByIdAsync(gameId, cancellationToken);
    }
} 