using System.Net;
using System.Text.Json;
using AuthService.API.Models;

namespace AuthService.API.Middleware;

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
            _logger.LogError(ex, "Unhandled exception for request {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (status, title, detail) = exception switch
        {
            ArgumentException => (HttpStatusCode.BadRequest, "Invalid request", exception.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized", "Access is denied"),
            InvalidOperationException => (HttpStatusCode.BadRequest, "Invalid operation", exception.Message),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred", "An internal server error occurred")
        };

        context.Response.StatusCode = (int)status;

        var problem = new ProblemDetails
        {
            Type = $"https://tools.ietf.org/html/rfc9110#section-15.5.{(int)status - 399}",
            Title = title,
            Status = (int)status,
            Detail = (int)status == 500 ? null : detail,
            Instance = context.Request.Path
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, options));
    }
}
