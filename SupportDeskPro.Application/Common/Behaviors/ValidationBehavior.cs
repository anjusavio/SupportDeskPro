/// <summary>
/// MediatR pipeline behavior that automatically runs FluentValidation
/// validators on every command and query before the handler executes.
/// If no validator exists for the request, execution continues normally.
/// If validation fails, throws ValidationException which is caught
/// by ExceptionMiddleware and returned as HTTP 400 with field-level errors.
/// Runs SECOND in the pipeline after logging, before handler execution.
/// </summary>
using FluentValidation;
using MediatR;

namespace SupportDeskPro.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Skip if no validators registered for this request type
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        // Run all validators and collect all failures
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}