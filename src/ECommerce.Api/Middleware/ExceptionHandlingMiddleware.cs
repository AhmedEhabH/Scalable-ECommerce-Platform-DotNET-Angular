using System.Net;
using System.Text.Json;
using FluentValidation;

namespace ECommerce.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred");

        var response = BuildErrorResponse(context, exception);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = response.StatusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }

    private ErrorResponse BuildErrorResponse(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;

        return exception switch
        {
            ValidationException validationEx => new ErrorResponse
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.BadRequest,
                Message = "Validation failed",
                Errors = validationEx.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()),
                TraceId = traceId
            },
            ArgumentException => new ErrorResponse
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.BadRequest,
                Message = exception.Message,
                TraceId = traceId
            },
            InvalidOperationException => new ErrorResponse
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.BadRequest,
                Message = exception.Message,
                TraceId = traceId
            },
            UnauthorizedAccessException => new ErrorResponse
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Message = "Authentication is required to access this resource",
                TraceId = traceId
            },
            KeyNotFoundException => new ErrorResponse
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.NotFound,
                Message = exception.Message,
                TraceId = traceId
            },
            _ => new ErrorResponse
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = _environment.IsDevelopment() ? exception.Message : "An unexpected error occurred. Please try again later",
                TraceId = traceId
            }
        };
    }
}

public class ErrorResponse
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? TraceId { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}
