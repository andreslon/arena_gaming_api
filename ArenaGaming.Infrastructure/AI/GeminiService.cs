using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ArenaGaming.Core.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ArenaGaming.Core.Domain;

namespace ArenaGaming.Infrastructure.AI;

public class GeminiService : IGeminiService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiService> _logger;
    private readonly HttpClient _httpClient;

    public GeminiService(IConfiguration configuration, ILogger<GeminiService> logger, HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<int> SuggestMoveAsync(Game game)
    {
        try
        {
            var apiKey = _configuration["Gemini_ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Gemini API key not configured, using fallback AI");
                return GetFallbackMove(game);
            }

            var prompt = GeneratePrompt(game);
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 50
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Updated endpoint with correct model
            var response = await _httpClient.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}",
                content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API request failed with status {StatusCode}: {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
                return GetFallbackMove(game);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Gemini API response: {Response}", responseContent);

            using var document = JsonDocument.Parse(responseContent);
            var root = document.RootElement;
            
            if (root.TryGetProperty("candidates", out var candidates) && 
                candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var content2) &&
                    content2.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0)
                {
                    var text = parts[0].GetProperty("text").GetString();
                    if (int.TryParse(text?.Trim(), out int position) && position >= 0 && position <= 8)
                    {
                        _logger.LogInformation("Gemini suggested move: {Position}", position);
                        return position;
                    }
                }
            }

            _logger.LogWarning("Could not parse valid move from Gemini response, using fallback");
            return GetFallbackMove(game);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API, using fallback AI");
            return GetFallbackMove(game);
        }
    }

    private string GeneratePrompt(Game game)
    {
        var board = game.Board;
        var currentPlayer = game.CurrentPlayerSymbol == 'X' ? "X" : "O";
        var opponent = currentPlayer == "X" ? "O" : "X";

        return $@"You are playing Tic-Tac-Toe as player {currentPlayer}. The board positions are numbered 0-8:
0|1|2
3|4|5
6|7|8

Current board state: '{board}'
- ' ' = empty
- 'X' = X player
- 'O' = O player

Your goal is to win or block the opponent from winning.
Return ONLY a single number (0-8) representing the position where you want to place your '{currentPlayer}' mark.";
    }

    private int GetFallbackMove(Game game)
    {
        try
        {
            var board = game.Board;
            var currentPlayer = game.CurrentPlayerSymbol;
            var opponent = currentPlayer == 'X' ? 'O' : 'X';

            // 1. Try to win
            for (int i = 0; i < 9; i++)
            {
                if (board[i] == ' ')
                {
                    var testBoard = board.ToCharArray();
                    testBoard[i] = currentPlayer;
                    if (IsWinningMove(new string(testBoard), currentPlayer))
                    {
                        _logger.LogInformation("Fallback AI found winning move at position {Position}", i);
                        return i;
                    }
                }
            }

            // 2. Block opponent from winning
            for (int i = 0; i < 9; i++)
            {
                if (board[i] == ' ')
                {
                    var testBoard = board.ToCharArray();
                    testBoard[i] = opponent;
                    if (IsWinningMove(new string(testBoard), opponent))
                    {
                        _logger.LogInformation("Fallback AI blocking opponent win at position {Position}", i);
                        return i;
                    }
                }
            }

            // 3. Take center if available
            if (board[4] == ' ')
            {
                _logger.LogInformation("Fallback AI taking center position");
                return 4;
            }

            // 4. Take corners
            int[] corners = { 0, 2, 6, 8 };
            foreach (int corner in corners)
            {
                if (board[corner] == ' ')
                {
                    _logger.LogInformation("Fallback AI taking corner position {Position}", corner);
                    return corner;
                }
            }

            // 5. Take any available position
            for (int i = 0; i < 9; i++)
            {
                if (board[i] == ' ')
                {
                    _logger.LogInformation("Fallback AI taking available position {Position}", i);
                    return i;
                }
            }

            _logger.LogWarning("No available moves found, returning position 0");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in fallback AI, returning position 0");
            return 0;
        }
    }

    private bool IsWinningMove(string board, char player)
    {
        int[,] winConditions = new int[,] {
            {0, 1, 2}, {3, 4, 5}, {6, 7, 8}, // rows
            {0, 3, 6}, {1, 4, 7}, {2, 5, 8}, // columns
            {0, 4, 8}, {2, 4, 6}             // diagonals
        };

        for (int i = 0; i < winConditions.GetLength(0); i++)
        {
            if (board[winConditions[i, 0]] == player &&
                board[winConditions[i, 1]] == player &&
                board[winConditions[i, 2]] == player)
            {
                return true;
            }
        }
        return false;
    }
} 