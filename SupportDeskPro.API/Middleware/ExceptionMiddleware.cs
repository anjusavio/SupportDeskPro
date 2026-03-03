/// <summary>
/// Global exception handling middleware implementing RFC 7807 Problem Details standard.
/// Sits at the outermost layer of the request pipeline and catches ALL unhandled exceptions.
/// Maps domain exceptions to appropriate HTTP status codes and returns structured JSON responses.
/// In Development: includes stack trace and full error details.
/// In Production: returns generic messages to avoid exposing internals.
/// Severity-based logging: business exceptions as Warning, unexpected as Error.
/// TraceId included in every response for log correlation.
/// </summary>
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SupportDeskPro.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace SupportDeskPro.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
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

    private async Task HandleExceptionAsync(
        HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        var path = context.Request.Path.ToString();
        var method = context.Request.Method;

        // Log with appropriate severity based on exception type
        LogException(exception, traceId, method, path);

        // Map exception to Problem Details response
        var problemDetails = exception switch
        {
            // FluentValidation failure — field level errors - 400 with field errors
            ValidationException validationEx => BuildValidationProblem(validationEx, path),

            // Business rule violation - 400 Bad Request
            BusinessValidationException businessEx
                => BuildProblem(
                    "https://supportdesk.com/errors/business-rule",
                    "Business Rule Violation",
                    HttpStatusCode.BadRequest,
                    businessEx.Message, path),

            // Resource not found - 404 Not Found
            NotFoundException notFoundEx
                => BuildProblem(
                    "https://supportdesk.com/errors/not-found",
                    "Resource Not Found",
                    HttpStatusCode.NotFound,
                    notFoundEx.Message, path),

            // Duplicate resource - 409 Conflict
            ConflictException conflictEx
                => BuildProblem(
                    "https://supportdesk.com/errors/conflict",
                    "Conflict",
                    HttpStatusCode.Conflict,
                    conflictEx.Message, path),

            // Insufficient permissions  -403 Forbidden
            ForbiddenException forbiddenEx
                => BuildProblem(
                    "https://supportdesk.com/errors/forbidden",
                    "Forbidden",
                    HttpStatusCode.Forbidden,
                    forbiddenEx.Message, path),

            // Unexpected server error - 500 Internal Server Error
            _ => BuildUnexpectedProblem(exception, path)
        };

        // Always include traceId for log correlation
        problemDetails.Extensions["traceId"] = traceId;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problemDetails.Status
            ?? (int)HttpStatusCode.InternalServerError;

        var json = JsonSerializer.Serialize(problemDetails,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition =
                    System.Text.Json.Serialization
                        .JsonIgnoreCondition.WhenWritingNull
            });

        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Builds RFC 7807 Problem Details response for FluentValidation failures.
    /// Groups errors by field name for easy frontend form validation handling.
    /// </summary>
    private static ProblemDetails BuildValidationProblem(
        ValidationException ex, string path)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        var details = new ProblemDetails
        {
            Type = "https://supportdesk.com/errors/validation",
            Title = "Validation Failed",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = "One or more validation errors occurred.",
            Instance = path
        };

        details.Extensions["errors"] = errors;
        return details;
    }

    /// <summary>
    /// Builds a standard RFC 7807 Problem Details response.
    /// </summary>
    private static ProblemDetails BuildProblem(
        string type, string title,
        HttpStatusCode statusCode,
        string detail, string path)
    {
        return new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = (int)statusCode,
            Detail = detail,
            Instance = path
        };
    }

    /// <summary>
    /// Builds Problem Details for unexpected exceptions.
    /// Shows full details in Development, generic message in Production.
    /// </summary>
    private ProblemDetails BuildUnexpectedProblem(
        Exception ex, string path)
    {
        var details = new ProblemDetails
        {
            Type = "https://supportdesk.com/errors/server-error",
            Title = "Internal Server Error",
            Status = (int)HttpStatusCode.InternalServerError,

            // Never expose internal errors in production
            Detail = _env.IsDevelopment()
                ? ex.Message
                : "An unexpected error occurred. Please try again.",

            Instance = path
        };

        // Stack trace visible in development only
        if (_env.IsDevelopment())
            details.Extensions["stackTrace"] = ex.StackTrace;

        return details;
    }

    /// <summary>
    /// Logs exceptions with appropriate severity.
    /// Expected business exceptions → Warning (not a system failure).
    /// Unexpected exceptions → Error (needs immediate attention).
    /// </summary>
    private void LogException(
        Exception exception,
        string traceId,
        string method,
        string path)
    {
        switch (exception)
        {
            case NotFoundException:
            case BusinessValidationException:
            case ConflictException:
            case ForbiddenException:
            case ValidationException:
                _logger.LogWarning(
                    "[{TraceId}] {ExceptionType} on {Method} {Path} — {Message}",
                    traceId,
                    exception.GetType().Name,
                    method, path,
                    exception.Message);
                break;

            default:
                _logger.LogError(exception,
                    "[{TraceId}] Unhandled {ExceptionType} on {Method} {Path}",
                    traceId,
                    exception.GetType().Name,
                    method, path);
                break;
        }
    }
}