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
            _logger.LogError(ex, "Ocurrió una excepción no manejada: {Message}", ex.Message);
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
                error = "Argumento inválido",
                message = "Los parámetros proporcionados son inválidos",
                details = exception.Message,
                statusCode = (int)HttpStatusCode.BadRequest
            },
            InvalidOperationException => new
            {
                error = "Operación inválida",
                message = "La operación solicitada no es válida en el estado actual",
                details = exception.Message,
                statusCode = (int)HttpStatusCode.BadRequest
            },
            UnauthorizedAccessException => new
            {
                error = "Acceso no autorizado",
                message = "No tienes permisos para realizar esta operación",
                details = exception.Message,
                statusCode = (int)HttpStatusCode.Unauthorized
            },
            KeyNotFoundException => new
            {
                error = "Recurso no encontrado",
                message = "El recurso solicitado no fue encontrado",
                details = exception.Message,
                statusCode = (int)HttpStatusCode.NotFound
            },
            TimeoutException => new
            {
                error = "Tiempo de espera agotado",
                message = "La operación tardó demasiado tiempo en completarse",
                details = exception.Message,
                statusCode = (int)HttpStatusCode.RequestTimeout
            },
            _ => new
            {
                error = "Error interno del servidor",
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
        // Analizar el tipo de excepción y proporcionar mensajes específicos
        var message = exception.Message.ToLower();

        if (message.Contains("connection") || message.Contains("timeout"))
        {
            return "Hay problemas de conectividad con los servicios externos. Por favor, intenta nuevamente.";
        }

        if (message.Contains("database") || message.Contains("sql") || message.Contains("npgsql"))
        {
            return "Hay un problema con la base de datos. Verifica que la base de datos esté disponible y configurada correctamente.";
        }

        if (message.Contains("redis") || message.Contains("cache"))
        {
            return "Hay un problema con el servicio de cache. Verifica que Redis esté disponible y configurado correctamente.";
        }

        if (message.Contains("pulsar") || message.Contains("broker"))
        {
            return "Hay un problema con el sistema de mensajería. Verifica que Pulsar esté disponible y configurado correctamente.";
        }

        if (message.Contains("gemini") || message.Contains("api"))
        {
            return "Hay un problema con el servicio de IA. Verifica que la API de Gemini esté configurada correctamente y que tengas una conexión a internet.";
        }

        if (message.Contains("serializ") || message.Contains("json"))
        {
            return "Hay un problema con el formato de los datos. Verifica que todos los campos estén correctamente formateados.";
        }

        return "Ocurrió un error inesperado. Verifica que todos los servicios estén configurados y funcionando correctamente.";
    }
} 