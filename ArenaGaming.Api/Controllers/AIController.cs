using Microsoft.AspNetCore.Mvc;
using ArenaGaming.Core.Application.Interfaces;
using ArenaGaming.Core.Domain;

namespace ArenaGaming.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AIController : ControllerBase
{
    private readonly IGeminiService _geminiService;

    public AIController(IGeminiService geminiService)
    {
        _geminiService = geminiService;
    }

    [HttpPost("suggest-move")]
    public async Task<IActionResult> SuggestMove([FromBody] SuggestMoveRequest request)
    {
        try
        {
            // Create a temporary game with the board state
            var game = new Game(Guid.NewGuid());
            game.SetBoardState(request.Board, request.CurrentPlayer);
            
            // Get AI suggestion
            var position = await _geminiService.SuggestMoveAsync(game);
            
            return Ok(new { position = position });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class SuggestMoveRequest
{
    public string Board { get; set; } = "";
    public char CurrentPlayer { get; set; }
} 