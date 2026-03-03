/// <summary>
/// MediatR pipeline behavior that provides structured logging for every command and query.
/// Automatically logs request start, successful completion with execution time,
/// slow request warnings (over 500ms), and full exception details on failure.
/// Runs FIRST in the pipeline before validation and handler execution.
/// </summary>
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace SupportDeskPro.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Generate short correlation ID for tracing request through logs
        var requestId = Guid.NewGuid().ToString()[..8];
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation(
            "[{RequestId}] START {RequestName}",
            requestId, requestName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            // Warn on slow requests — helps identify performance bottlenecks
            if (stopwatch.ElapsedMilliseconds > 500)
            {
                _logger.LogWarning(
                    "[{RequestId}] SLOW REQUEST {RequestName} " +
                    "completed in {ElapsedMs}ms — consider optimization",
                    requestId, requestName, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "[{RequestId}] END {RequestName} " +
                    "completed in {ElapsedMs}ms",
                    requestId, requestName, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "[{RequestId}] ERROR {RequestName} " +
                "failed after {ElapsedMs}ms — {ErrorMessage}",
                requestId, requestName,
                stopwatch.ElapsedMilliseconds, ex.Message);

            // Re-throw — let ExceptionMiddleware handle the HTTP response
            throw;
        }
    }
}