using System;
using System.Threading.Tasks;
using ArenaGaming.Core.Application.Interfaces;
using ArenaGaming.Core.Domain;
using Microsoft.AspNetCore.Mvc;
using ArenaGaming.Infrastructure.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArenaGaming.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestAIController : ControllerBase
{
    private readonly IGeminiService _geminiService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TestAIController> _logger;

    public TestAIController(IGeminiService geminiService, IConfiguration configuration, ILogger<TestAIController> logger)
    {
        _geminiService = geminiService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("simple")]
    public async Task<IActionResult> TestSimple([FromBody] TestAIRequest request)
    {
        try
        {
            // Create a temp game with specific board state
            var game = new Game(Guid.NewGuid());
            game.SetBoardState(request.Board, request.CurrentPlayer);

            Console.WriteLine($"[DEBUG] Testing AI with board: '{request.Board}' and player: '{request.CurrentPlayer}'");

            var aiMove = await _geminiService.SuggestMoveAsync(game);

            Console.WriteLine($"[DEBUG] AI suggested move: {aiMove}");

            return Ok(new
            {
                board = request.Board,
                currentPlayer = request.CurrentPlayer,
                aiSuggestedMove = aiMove,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Error in AI test: {ex.Message}");
            return BadRequest(new
            {
                error = ex.Message,
                board = request.Board,
                currentPlayer = request.CurrentPlayer
            });
        }
    }

    [HttpPost("direct")]
    public async Task<IActionResult> TestDirect([FromBody] TestAIRequest request)
    {
        try
        {
            // Create GeminiService directly, bypassing DI
            using var httpClient = new HttpClient();
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var geminiLogger = loggerFactory.CreateLogger<GeminiService>();
            var geminiService = new GeminiService(_configuration, geminiLogger, httpClient);
            
            // Create a temp game with specific board state
            var game = new Game(Guid.NewGuid());
            game.SetBoardState(request.Board, request.CurrentPlayer);

            Console.WriteLine($"[DIRECT] Testing AI with board: '{request.Board}' and player: '{request.CurrentPlayer}'");

            var aiMove = await geminiService.SuggestMoveAsync(game);

            Console.WriteLine($"[DIRECT] AI suggested move: {aiMove}");

            return Ok(new
            {
                board = request.Board,
                currentPlayer = request.CurrentPlayer,
                aiSuggestedMove = aiMove,
                timestamp = DateTime.UtcNow,
                method = "DIRECT"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DIRECT] Error in AI test: {ex.Message}");
            return BadRequest(new
            {
                error = ex.Message,
                board = request.Board,
                currentPlayer = request.CurrentPlayer,
                method = "DIRECT"
            });
        }
    }
}

public class TestAIRequest
{
    public string Board { get; set; } = "         ";
    public char CurrentPlayer { get; set; } = 'O';
} 