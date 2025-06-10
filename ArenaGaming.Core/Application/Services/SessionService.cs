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
        try
        {
            Console.WriteLine($"[GetSessionAsync] Looking for session: {sessionId}");
            
            // Try to get from cache first
            try
            {
                var cachedSession = await _cacheService.GetAsync<Session>($"session:{sessionId}", cancellationToken);
                if (cachedSession != null)
                {
                    Console.WriteLine($"[GetSessionAsync] Session found in cache: {sessionId}");
                    return cachedSession;
                }
                Console.WriteLine($"[GetSessionAsync] Session not found in cache, searching in database");
            }
            catch (Exception cacheEx)
            {
                Console.WriteLine($"[GetSessionAsync] Error accessing cache: {cacheEx.Message}");
                // Continue to database lookup
            }

            // If not in cache, get from database
            var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
            if (session != null)
            {
                Console.WriteLine($"[GetSessionAsync] Session found in database: {sessionId}");
                // Cache the session
                try
                {
                    await _cacheService.SetAsync($"session:{sessionId}", session, TimeSpan.FromHours(1), cancellationToken);
                    Console.WriteLine($"[GetSessionAsync] Session saved to cache");
                }
                catch (Exception cacheEx)
                {
                    Console.WriteLine($"[GetSessionAsync] Error saving to cache: {cacheEx.Message}");
                    // Continue execution - cache errors shouldn't break the flow
                }
            }
            else
            {
                Console.WriteLine($"[GetSessionAsync] Session not found in database: {sessionId}");
            }

            return session;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetSessionAsync] General error: {ex.Message}");
            Console.WriteLine($"[GetSessionAsync] StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<Game> StartNewGameAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"[StartNewGameAsync] Starting new game for session: {sessionId}");
            
            var session = await GetSessionAsync(sessionId, cancellationToken);
            if (session == null)
            {
                Console.WriteLine($"[StartNewGameAsync] Session not found: {sessionId}");
                throw new ArgumentException($"Session not found: {sessionId}", nameof(sessionId));
            }

            Console.WriteLine($"[StartNewGameAsync] Session found: {session.Id}, PlayerId: {session.PlayerId}");
            
            var game = new Game(session.PlayerId);
            Console.WriteLine($"[StartNewGameAsync] Creating game with ID: {game.Id}");
            
            await _gameRepository.AddAsync(game, cancellationToken);
            Console.WriteLine($"[StartNewGameAsync] Game saved to repository");

            session.CurrentGameId = game.Id;
            await _sessionRepository.UpdateAsync(session, cancellationToken);
            Console.WriteLine($"[StartNewGameAsync] Session updated with CurrentGameId: {game.Id}");

            // Update cache
            try
            {
                await _cacheService.SetAsync($"session:{sessionId}", session, TimeSpan.FromHours(1), cancellationToken);
                await _cacheService.SetAsync($"game:{game.Id}", game, TimeSpan.FromMinutes(30), cancellationToken);
                Console.WriteLine($"[StartNewGameAsync] Cache updated");
            }
            catch (Exception cacheEx)
            {
                Console.WriteLine($"[StartNewGameAsync] Error updating cache: {cacheEx.Message}");
                // Continue execution - cache errors shouldn't break the flow
            }

            // Publish game started event
            try
            {
                var gameStartedEvent = new GameStartedEvent(game.Id, session.PlayerId);
                await _eventPublisher.PublishAsync(gameStartedEvent, "game-started", cancellationToken);
                Console.WriteLine($"[StartNewGameAsync] 'game-started' event published");
            }
            catch (Exception eventEx)
            {
                Console.WriteLine($"[StartNewGameAsync] Error publishing event: {eventEx.Message}");
                // Continue execution - event publishing errors shouldn't break the flow
            }

            Console.WriteLine($"[StartNewGameAsync] Game started successfully: {game.Id}");
            return game;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StartNewGameAsync] General error: {ex.Message}");
            Console.WriteLine($"[StartNewGameAsync] StackTrace: {ex.StackTrace}");
            throw;
        }
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