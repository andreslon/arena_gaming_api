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
        // FORCE the API key for debugging
        var apiKey = "AIzaSyAxMlZ80j89_vjoqXAu__1NmYC4yi2bzmg";
        
        // NEVER use fallback - force Gemini or throw exception
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new Exception("NO API KEY - FORCED FAILURE");
        }

        try
        {
            _logger.LogInformation("=== GEMINI AI CALL ===");
            _logger.LogInformation("Board: '{Board}'", game.Board);
            _logger.LogInformation("Player: '{Player}'", game.CurrentPlayerSymbol);

            var randomId = Guid.NewGuid().ToString()[0..8];
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var randomSeed = new Random().Next(1000, 9999);
            
            var attitudes = new[] {
                "You're a RUTHLESS opponent who plays to DESTROY",
                "You're an AGGRESSIVE player who shows NO MERCY", 
                "You're a CUNNING strategist who OUTSMARTS opponents",
                "You're a COMPETITIVE beast who CRUSHES enemies",
                "You're a TACTICAL genius who DOMINATES games"
            };
            var attitude = attitudes[new Random().Next(attitudes.Length)];
            
            var strategies = new[] {
                "Be UNPREDICTABLE - surprise your opponent",
                "Play AGGRESSIVELY - attack at every opportunity", 
                "Think STRATEGICALLY - set deadly traps",
                "Stay FLEXIBLE - adapt to counter their moves",
                "Be RELENTLESS - never give them a break"
            };
            var strategy = strategies[new Random().Next(strategies.Length)];
            
            var prompt = $@"🎯 BATTLE #{randomId} | SEED:{randomSeed} | {timestamp}

{attitude}! You are '{game.CurrentPlayerSymbol}' and you WILL WIN!

BATTLEFIELD:
{game.Board[0]}|{game.Board[1]}|{game.Board[2]}
{game.Board[3]}|{game.Board[4]}|{game.Board[5]}
{game.Board[6]}|{game.Board[7]}|{game.Board[8]}

MAP (0-8):
0|1|2
3|4|5
6|7|8

🔥 MISSION: {strategy}

COMBAT PRIORITIES:
1. 🏆 KILL SHOT - Win immediately if possible
2. 🛡️ DEFENSE - Block enemy victory at ALL COSTS  
3. ⚔️ ATTACK - Control center(4) or corners(0,2,6,8)
4. 🎲 CHAOS - Be unpredictable, vary your tactics

CRUSH them! Your killing move (0-8):";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                },
                generationConfig = new
                {
                    temperature = 1.2,  // Even higher than max for variation
                    maxOutputTokens = 100,
                    topK = 5,
                    topP = 0.6,
                    candidateCount = 1
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("🚀 Calling Gemini API with temperature 0.9...");

            var response = await _httpClient.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}",
                content);

            _logger.LogInformation("🌐 HTTP Response: {Status}", response.StatusCode);
            Console.WriteLine($"*** HTTP STATUS: {response.StatusCode} ***");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API failed: {Status} - {Error}", response.StatusCode, errorContent);
                return GetIntelligentFallback(game.Board, game.CurrentPlayerSymbol);
            }

            var responseText = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Gemini response: {Response}", responseText);

            // Parse JSON response
            using var doc = JsonDocument.Parse(responseText);
            if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("content", out var content2) &&
                content2.TryGetProperty("parts", out var parts) &&
                parts.GetArrayLength() > 0)
            {
                var text = parts[0].GetProperty("text").GetString()?.Trim();
                _logger.LogInformation("Gemini text: '{Text}'", text);

                // Try to parse the number
                if (int.TryParse(text, out int position) && position >= 0 && position <= 8)
                {
                    _logger.LogInformation("🎯 GEMINI SUCCESS: Position {Position} from direct parse", position);
                    Console.WriteLine($"*** GEMINI USED: {position} ***");
                    return position;
                }

                // Try to find first digit
                foreach (char c in text ?? "")
                {
                    if (char.IsDigit(c))
                    {
                        int digit = c - '0';
                        if (digit >= 0 && digit <= 8)
                        {
                            _logger.LogInformation("🎯 GEMINI EXTRACTED: Position {Position} from text", digit);
                            return digit;
                        }
                    }
                }
            }

            _logger.LogWarning("Could not parse Gemini response: {Response}", responseText);
            return GetIntelligentFallback(game.Board, game.CurrentPlayerSymbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini failed, using intelligent fallback");
            Console.WriteLine($"*** GEMINI FAILED: {ex.Message} - Using fallback ***");
            return GetIntelligentFallback(game.Board, game.CurrentPlayerSymbol);
        }
    }

    private int GetIntelligentFallback(string board, char currentPlayer)
    {
        _logger.LogWarning("🔥 AGGRESSIVE FALLBACK AI ACTIVATED");
        
        // Find available positions
        var available = new List<int>();
        for (int i = 0; i < 9; i++)
        {
            if (board[i] == ' ')
                available.Add(i);
        }

        if (available.Count == 0) return 0;

        char opponent = currentPlayer == 'X' ? 'O' : 'X';

        // 1. KILL SHOT - Check for winning move
        foreach (int pos in available)
        {
            var testBoard = board.ToCharArray();
            testBoard[pos] = currentPlayer;
            if (IsWinningMove(new string(testBoard), currentPlayer))
            {
                _logger.LogInformation("💀 FALLBACK KILL SHOT: {Position}", pos);
                return pos;
            }
        }

        // 2. DEFENSE - Check for blocking move
        foreach (int pos in available)
        {
            var testBoard = board.ToCharArray();
            testBoard[pos] = opponent;
            if (IsWinningMove(new string(testBoard), opponent))
            {
                _logger.LogInformation("🛡️ FALLBACK BLOCKING: {Position}", pos);
                return pos;
            }
        }

        // 3. AGGRESSIVE STRATEGY - Mix up the approach
        var random = new Random();
        var strategy = random.Next(3);
        
        if (strategy == 0 && available.Contains(4))
        {
            _logger.LogInformation("⚔️ FALLBACK DOMINATING CENTER");
            return 4;
        }
        
        // 4. ATTACK CORNERS aggressively
        var corners = new[] { 0, 2, 6, 8 };
        var availableCorners = corners.Where(available.Contains).ToArray();
        if (availableCorners.Length > 0)
        {
            var corner = availableCorners[random.Next(availableCorners.Length)];
            _logger.LogInformation("🎯 FALLBACK CORNER ASSAULT: {Position}", corner);
            return corner;
        }

        // 5. CHAOS MODE - Take center if still available or random
        if (available.Contains(4))
        {
            _logger.LogInformation("🌪️ FALLBACK CHAOS CENTER");
            return 4;
        }

        // 6. UNPREDICTABLE ATTACK
        var randomPos = available[random.Next(available.Count)];
        _logger.LogInformation("🎲 FALLBACK RANDOM STRIKE: {Position}", randomPos);
        return randomPos;
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