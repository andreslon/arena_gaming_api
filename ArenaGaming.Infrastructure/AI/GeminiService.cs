using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ArenaGaming.Core.Application.Interfaces;

namespace ArenaGaming.Infrastructure.AI;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";

    public GeminiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _apiKey = Environment.GetEnvironmentVariable("Gemini_ApiKey") ?? throw new ArgumentNullException("Gemini_ApiKey environment variable is missing");
    }

    public async Task<int> GetNextMoveAsync(string board, char playerSymbol, CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrompt(board, playerSymbol);
        
        var request = new
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
            }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync($"{ApiUrl}?key={_apiKey}", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        var prediction = responseJson
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrEmpty(prediction))
        {
            return GetFallbackMove(board);
        }

        // Parse the prediction to get the move position
        if (int.TryParse(prediction.Trim(), out int position) && position >= 0 && position < 9)
        {
            return position;
        }

        // If parsing fails, use a simple fallback strategy
        return GetFallbackMove(board);
    }

    private string BuildPrompt(string board, char playerSymbol)
    {
        return $"You are playing Tic Tac Toe. The current board state is: {board}. " +
               $"You are playing as {playerSymbol}. " +
               $"Return only the number (0-8) of the position where you want to make your move.";
    }

    private int GetFallbackMove(string board)
    {
        // Simple strategy: find the first empty position
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] == ' ')
                return i;
        }
        return 0; // Should never reach here if the game is not over
    }
} 