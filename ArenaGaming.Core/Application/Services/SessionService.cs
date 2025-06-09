using System;
using System.Threading;
using System.Threading.Tasks;
using ArenaGaming.Core.Application.Interfaces;
using ArenaGaming.Core.Domain;
using ArenaGaming.Core.Domain.Events;

namespace ArenaGaming.Core.Application.Services;

public class SessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IGeminiService _geminiService;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICacheService _cacheService;

    public SessionService(
        ISessionRepository sessionRepository,
        IGameRepository gameRepository,
        IGeminiService geminiService,
        IEventPublisher eventPublisher,
        ICacheService cacheService)
    {
        _sessionRepository = sessionRepository;
        _gameRepository = gameRepository;
        _geminiService = geminiService;
        _eventPublisher = eventPublisher;
        _cacheService = cacheService;
    }

    public async Task<Session> CreateSessionAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var session = new Session(playerId);
        await _sessionRepository.AddAsync(session, cancellationToken);
        
        // Cache the session
        await _cacheService.SetAsync($"session:{session.Id}", session, TimeSpan.FromHours(1), cancellationToken);

        return session;
    }

    public async Task<Session?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        // Try to get from cache first
        var cachedSession = await _cacheService.GetAsync<Session>($"session:{sessionId}", cancellationToken);
        if (cachedSession != null)
            return cachedSession;

        // If not in cache, get from database
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session != null)
        {
            // Cache the session
            await _cacheService.SetAsync($"session:{sessionId}", session, TimeSpan.FromHours(1), cancellationToken);
        }

        return session;
    }

    public async Task<Game> StartNewGameAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        if (session == null)
            throw new ArgumentException("Session not found", nameof(sessionId));

        var game = new Game(session.PlayerId);
        await _gameRepository.AddAsync(game, cancellationToken);

        session.CurrentGameId = game.Id;
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        // Update cache
        await _cacheService.SetAsync($"session:{sessionId}", session, TimeSpan.FromHours(1), cancellationToken);
        await _cacheService.SetAsync($"game:{game.Id}", game, TimeSpan.FromMinutes(30), cancellationToken);

        // Publish game started event
        var gameStartedEvent = new GameStartedEvent(game.Id, session.PlayerId);
        await _eventPublisher.PublishAsync(gameStartedEvent, "game-started", cancellationToken);

        return game;
    }

    public async Task<Game> MakeAiMoveAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        if (session == null)
            throw new ArgumentException("Session not found", nameof(sessionId));

        if (!session.CurrentGameId.HasValue)
            throw new InvalidOperationException("No active game in session");

        var game = await _gameRepository.GetByIdAsync(session.CurrentGameId.Value, cancellationToken);
        if (game == null)
            throw new ArgumentException("Game not found", nameof(session.CurrentGameId));

        // Get AI move
        var aiPosition = await _geminiService.GetNextMoveAsync(new string(game.Board), game.CurrentPlayerSymbol, cancellationToken);
        
        // Make the move
        game.MakeMove(aiPosition, null); // null indicates AI player
        await _gameRepository.UpdateAsync(game, cancellationToken);

        // Update cache
        await _cacheService.SetAsync($"game:{game.Id}", game, TimeSpan.FromMinutes(30), cancellationToken);

        // Publish events
        var moveEvent = new MoveMadeEvent(game.Id, Guid.Empty, aiPosition, new string(game.Board));
        await _eventPublisher.PublishAsync(moveEvent, "move-made", cancellationToken);

        var geminiEvent = new GeminiMoveEvent(sessionId, aiPosition, "AI move made");
        await _eventPublisher.PublishAsync(geminiEvent, "gemini-move", cancellationToken);

        if (game.Status == GameStatus.Ended)
        {
            var gameEndedEvent = new GameEndedEvent(game.Id, game.WinnerId, game.WinnerId == null);
            await _eventPublisher.PublishAsync(gameEndedEvent, "game-ended", cancellationToken);
        }

        return game;
    }
} 