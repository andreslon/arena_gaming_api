using System;
using System.Threading;
using System.Threading.Tasks;
using ArenaGaming.Core.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArenaGaming.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly SessionService _sessionService;

    public SessionsController(SessionService sessionService)
    {
        _sessionService = sessionService;
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
            message = "Juego iniciado exitosamente",
            data = game 
        });
    }

    [HttpPost("{id}/ai-move")]
    public async Task<IActionResult> MakeAiMove(Guid id, CancellationToken cancellationToken)
    {
        var game = await _sessionService.MakeAiMoveAsync(id, cancellationToken);
        return Ok(new { 
            success = true, 
            message = "Movimiento de IA realizado exitosamente",
            data = game 
        });
    }
}

public class CreateSessionRequest
{
    public Guid PlayerId { get; set; }
} 