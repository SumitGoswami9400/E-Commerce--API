using System.Net;
using System.Text.Json;
using ECommerceAPI.DTOs;

namespace ECommerceAPI.Middleware;

/// <summary>
/// Global Exception Handling Middleware
/// Catches ALL unhandled exceptions and returns consistent JSON responses.
/// This prevents stack traces from leaking to clients in production.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next   = next;
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
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            ArgumentNullException      => (HttpStatusCode.BadRequest, "Invalid request: required value is missing."),
            UnauthorizedAccessException=> (HttpStatusCode.Unauthorized, "You are not authorized to perform this action."),
            KeyNotFoundException       => (HttpStatusCode.NotFound, "The requested resource was not found."),
            InvalidOperationException  => (HttpStatusCode.BadRequest, exception.Message),
            _                         => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.Fail(message);
        var options  = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}
