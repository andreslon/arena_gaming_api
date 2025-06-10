using System;
using System.Threading;
using System.Threading.Tasks;
using ArenaGaming.Core.Application.Services;
using ArenaGaming.Core.Domain;
using Microsoft.AspNetCore.Mvc;

namespace ArenaGaming.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    private readonly GameService _gameService;

    public GamesController(GameService gameService)
    {
        _gameService = gameService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateGame([FromBody] CreateGameRequest request, CancellationToken cancellationToken)
    {
        var game = await _gameService.CreateGameAsync(request.PlayerId, cancellationToken);
        return Ok(new GameResponse(game));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGame(Guid id, CancellationToken cancellationToken)
    {
        var game = await _gameService.GetGameAsync(id, cancellationToken);
        if (game == null)
            return NotFound();

        return Ok(new GameResponse(game));
    }

    [HttpPost("{id}/moves")]
    public async Task<IActionResult> MakeMove(Guid id, [FromBody] MakeMoveRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var game = await _gameService.MakeMoveAsync(id, request.PlayerId, request.Position, cancellationToken);
            return Ok(new GameResponse(game));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid argument", message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("concurrent modifications"))
        {
            return Conflict(new 
            { 
                error = "Concurrency conflict", 
                message = "The game was modified by another player. Please refresh and try again.",
                details = ex.Message 
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = "Invalid operation", message = ex.Message });
        }
    }
}

public class CreateGameRequest
{
    public Guid PlayerId { get; set; }
}

public class MakeMoveRequest
{
    public Guid PlayerId { get; set; }
    public int Position { get; set; }
}

public class GameResponse
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public string Board { get; set; }
    public string CurrentPlayerSymbol { get; set; }
    public string Status { get; set; }
    public Guid? WinnerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public GameResponse(Game game)
    {
        Id = game.Id;
        PlayerId = game.PlayerId;
        Board = new string(game.Board);
        CurrentPlayerSymbol = game.CurrentPlayerSymbol.ToString();
        Status = game.Status.ToString();
        WinnerId = game.WinnerId;
        CreatedAt = game.CreatedAt;
        EndedAt = game.EndedAt;
        UpdatedAt = game.UpdatedAt;
    }
} 