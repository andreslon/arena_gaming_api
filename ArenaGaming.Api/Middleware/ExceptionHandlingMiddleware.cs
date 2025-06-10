using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace ArenaGaming.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = exception switch
        {
            ArgumentException or ArgumentNullException => new
            {
                error = "Invalid argument",
                message = "The provided parameters are invalid",
                details = exception.Message,
                statusCode = (int)HttpStatusCode.BadRequest
            },
            InvalidOperationException => new
            {
                error = "Invalid operation",
                message = "The requested operation is not valid in the current state",
                details = exception.Message,
                statusCode = (int)HttpStatusCode.BadRequest
            },
            UnauthorizedAccessException => new
            {
                error = "Unauthorized access",
                message = "You do not have permission to perform this operation",
                details = exception.Message,
                statusCode = (int)HttpStatusCode.Unauthorized
            },
            KeyNotFoundException => new
            {
                error = "Resource not found",
                message = "The requested resource was not found",
                details = exception.Message,
                statusCode = (int)HttpStatusCode.NotFound
            },
            TimeoutException => new
            {
                error = "Timeout exceeded",
                message = "The operation took too long to complete",
                details = exception.Message,
                statusCode = (int)HttpStatusCode.RequestTimeout
            },
            _ => new
            {
                error = "Internal server error",
                message = GetFriendlyErrorMessage(exception),
                details = exception.Message,
                statusCode = (int)HttpStatusCode.InternalServerError
            }
        };

        response.StatusCode = errorResponse.statusCode;

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(jsonResponse);
    }

    private static string GetFriendlyErrorMessage(Exception exception)
    {
        // Analyze the exception type and provide specific messages
        var message = exception.Message.ToLower();

        if (message.Contains("connection") || message.Contains("timeout"))
        {
            return "There are connectivity issues with external services. Please try again.";
        }

        if (message.Contains("database") || message.Contains("sql") || message.Contains("npgsql"))
        {
            return "There is a problem with the database. Verify that the database is available and properly configured.";
        }

        if (message.Contains("redis") || message.Contains("cache"))
        {
            return "There is a problem with the cache service. Verify that Redis is available and properly configured.";
        }

        if (message.Contains("pulsar") || message.Contains("broker"))
        {
            return "There is a problem with the messaging system. Verify that Pulsar is available and properly configured.";
        }

        if (message.Contains("gemini") || message.Contains("api"))
        {
            return "There is a problem with the AI service. Verify that the Gemini API is properly configured and that you have an internet connection.";
        }

        if (message.Contains("serializ") || message.Contains("json"))
        {
            return "There is a problem with the data format. Verify that all fields are properly formatted.";
        }

        return "An unexpected error occurred. Verify that all services are configured and working properly.";
    }
} 