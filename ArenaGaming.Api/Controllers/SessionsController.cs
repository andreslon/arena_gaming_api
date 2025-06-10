using System;
using System.Threading;
using System.Threading.Tasks;
using ArenaGaming.Core.Application.Services;
using ArenaGaming.Core.Application.Interfaces;
using ArenaGaming.Core.Domain;
using Microsoft.AspNetCore.Mvc;

namespace ArenaGaming.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly SessionService _sessionService;
    private readonly IGeminiService _geminiService;

    public SessionsController(SessionService sessionService, IGeminiService geminiService)
    {
        _sessionService = sessionService;
        _geminiService = geminiService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await _sessionService.CreateSessionAsync(request.PlayerId, cancellationToken);
        return Ok(session);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSession(Guid id, CancellationToken cancellationToken)
    {
        var session = await _sessionService.GetSessionAsync(id, cancellationToken);
        if (session == null)
            return NotFound();

        return Ok(session);
    }

    [HttpPost("{id}/init-game")]
    public async Task<IActionResult> InitGame(Guid id, CancellationToken cancellationToken)
    {
        var game = await _sessionService.StartNewGameAsync(id, cancellationToken);
        return Ok(new { 
            success = true, 
            message = "Game started successfully",
            data = game 
        });
    }

    [HttpPost("{id}/ai-move")]
    public async Task<IActionResult> MakeAiMove(Guid id, CancellationToken cancellationToken)
    {
        var game = await _sessionService.MakeAiMoveAsync(id, cancellationToken);
        return Ok(new { 
            success = true, 
            message = "AI move performed successfully",
            data = game 
        });
    }

    /// <summary>
    /// Get AI move suggestion without managing game state
    /// Use this endpoint when managing game state locally in frontend
    /// </summary>
    [HttpPost("ai-move-only")]
    public async Task<IActionResult> GetAiMoveOnly([FromBody] AiMoveRequest request)
    {
        try
        {
            // Create a temporary game object just for AI calculation
            var tempGame = new Game(Guid.NewGuid());
            
            // Set the board state from request
            tempGame.SetBoardState(request.Board, request.CurrentPlayerSymbol);
            
            // Get AI suggestion
            var aiPosition = await _geminiService.SuggestMoveAsync(tempGame);
            
            return Ok(new
            {
                success = true,
                position = aiPosition,
                message = "AI move calculated successfully"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                message = $"Error calculating AI move: {ex.Message}"
            });
        }
    }
}

public class CreateSessionRequest
{
    public Guid PlayerId { get; set; }
}

public class AiMoveRequest
{
    public string Board { get; set; } = "         "; // 9 characters: X, O, or space
    public char CurrentPlayerSymbol { get; set; } = 'O'; // Usually AI plays as O
} 