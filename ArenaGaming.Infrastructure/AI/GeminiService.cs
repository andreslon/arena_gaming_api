using System;
using System.Collections.Generic;
using System.Linq;
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

            _logger.LogInformation("Asking Gemini for move suggestion. Player: {Player}, Board: {Board}", 
                game.CurrentPlayerSymbol, game.Board);

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
                    temperature = 0.0,  // Maximum deterministic for logical game moves
                    maxOutputTokens = 5,  // We only need 1 digit
                    candidateCount = 1
                },
                safetySettings = new[]
                {
                    new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_NONE" },
                    new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_NONE" },
                    new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_NONE" },
                    new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_NONE" }
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Set timeout for faster fallback
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            // Updated endpoint with correct model
            var response = await _httpClient.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}",
                content, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API request failed with status {StatusCode}: {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
                return GetFallbackMove(game);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Gemini API response received. Length: {Length}", responseContent?.Length ?? 0);
            _logger.LogDebug("Gemini API full response: {Response}", responseContent);

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
                    _logger.LogInformation("Gemini raw text response: '{Text}'", text);
                    
                    // Try to extract number from response
                    var cleanText = text?.Trim();
                    
                    // Try direct parsing first
                    if (int.TryParse(cleanText, out int position) && IsValidPosition(position))
                    {
                        _logger.LogInformation("Gemini suggested move: {Position}", position);
                        return position;
                    }
                    
                    // Try to extract first digit if response contains extra text
                    if (!string.IsNullOrEmpty(cleanText))
                    {
                        foreach (char c in cleanText)
                        {
                            if (char.IsDigit(c))
                            {
                                int digit = c - '0';
                                if (IsValidPosition(digit))
                                {
                                    _logger.LogInformation("Gemini extracted move from text: {Position}", digit);
                                    return digit;
                                }
                            }
                        }
                    }
                    
                    _logger.LogWarning("Gemini response '{Text}' contains no valid move position", cleanText);
                }
                else
                {
                    _logger.LogWarning("Gemini response has unexpected structure - no content/parts found");
                }
            }
            else
            {
                _logger.LogWarning("Gemini response has no candidates");
            }

            _logger.LogWarning("Could not parse valid move from Gemini response, using fallback AI");
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
        var currentPlayer = game.CurrentPlayerSymbol;
        var opponent = currentPlayer == 'X' ? 'O' : 'X';

        // Create visual board representation
        var visualBoard = "";
        for (int i = 0; i < 9; i += 3)
        {
            visualBoard += $"{board[i]}|{board[i + 1]}|{board[i + 2]}\n";
            if (i < 6) visualBoard += "-----\n";
        }

        return $@"You are a MASTER Tic-Tac-Toe player. You are '{currentPlayer}', opponent is '{opponent}'.

BOARD STATE:
{visualBoard}

POSITIONS:
0|1|2
-----
3|4|5
-----
6|7|8

STRATEGY (in priority order):
1. WIN IMMEDIATELY if you can complete 3-in-a-row
2. BLOCK opponent from winning
3. CREATE FORK (multiple winning paths)
4. BLOCK opponent forks
5. TAKE CENTER (position 4) if available
6. TAKE CORNERS (0,2,6,8) over edges (1,3,5,7)

Current situation analysis:
- Look for any row/column/diagonal with 2 '{currentPlayer}' and 1 empty space → WIN
- Look for any row/column/diagonal with 2 '{opponent}' and 1 empty space → BLOCK
- Otherwise follow strategic priority

RESPOND WITH ONLY THE POSITION NUMBER (0-8). NO OTHER TEXT.";
    }

    private int GetFallbackMove(Game game)
    {
        try
        {
            var board = game.Board;
            var currentPlayer = game.CurrentPlayerSymbol;

            _logger.LogWarning("Using simple fallback AI since Gemini failed");

            // Simple fallback: just find any empty position
            var availablePositions = new List<int>();
            for (int i = 0; i < 9; i++)
            {
                if (board[i] == ' ')
                    availablePositions.Add(i);
            }

            if (availablePositions.Count == 0)
            {
                _logger.LogError("No available moves found");
                return 0;
            }

            // Prefer center, then corners, then any position
            if (availablePositions.Contains(4))
            {
                _logger.LogInformation("Fallback AI taking center");
                return 4;
            }

            var corners = new[] { 0, 2, 6, 8 };
            var availableCorners = corners.Where(c => availablePositions.Contains(c)).ToArray();
            if (availableCorners.Length > 0)
            {
                var corner = availableCorners[new Random().Next(availableCorners.Length)];
                _logger.LogInformation("Fallback AI taking corner {Position}", corner);
                return corner;
            }

            // Any available position
            var position = availablePositions[new Random().Next(availablePositions.Count)];
            _logger.LogInformation("Fallback AI taking position {Position}", position);
            return position;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in fallback AI, returning position 0");
            return 0;
        }
    }

    private bool IsValidPosition(int position)
    {
        return position >= 0 && position <= 8;
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