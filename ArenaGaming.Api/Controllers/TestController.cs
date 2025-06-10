using Microsoft.AspNetCore.Mvc;
using ArenaGaming.Core.Domain;

namespace ArenaGaming.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet("game-logic")]
    public IActionResult TestGameLogic()
    {
        try
        {
            var playerId = Guid.NewGuid();
            var game = new Game(playerId);
            
            var initialState = new {
                Board = game.Board,
                BoardLength = game.Board.Length,
                Status = game.Status.ToString(),
                CurrentPlayer = game.CurrentPlayerSymbol.ToString()
            };
            
            // Test first move
            game.MakeMove(0, playerId);
            
            var afterFirstMove = new {
                Board = game.Board,
                BoardLength = game.Board.Length,
                Status = game.Status.ToString(),
                CurrentPlayer = game.CurrentPlayerSymbol.ToString(),
                Position0Value = game.Board[0].ToString(),
                Position0ASCII = (int)game.Board[0]
            };
            
            // Test duplicate move (should fail)
            string duplicateError = null;
            try 
            {
                game.MakeMove(0, playerId);
            }
            catch (InvalidOperationException ex)
            {
                duplicateError = ex.Message;
            }
            
            // Test second move
            game.MakeMove(1, playerId);
            
            var afterSecondMove = new {
                Board = game.Board,
                BoardLength = game.Board.Length,
                Status = game.Status.ToString(),
                CurrentPlayer = game.CurrentPlayerSymbol.ToString()
            };
            
            return Ok(new {
                InitialState = initialState,
                AfterFirstMove = afterFirstMove,
                DuplicateError = duplicateError,
                AfterSecondMove = afterSecondMove,
                Success = true
            });
        }
        catch (Exception ex)
        {
            return Ok(new {
                Error = ex.Message,
                StackTrace = ex.StackTrace,
                Success = false
            });
        }
    }
} 