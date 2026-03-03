/// <summary>
/// Application layer service registration.
/// Registers MediatR with all handlers, FluentValidation validators,
/// and pipeline behaviors in correct execution order:
/// LoggingBehavior runs first → ValidationBehavior runs second → Handler executes last.
/// </summary>
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SupportDeskPro.Application.Common.Behaviors;

namespace SupportDeskPro.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        // Register MediatR — scans assembly for all handlers
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(
                typeof(DependencyInjection).Assembly));

        // Register all FluentValidation validators in assembly
        services.AddValidatorsFromAssembly(
            typeof(DependencyInjection).Assembly);

        // Register pipeline behaviors — ORDER IS CRITICAL
        // 1. Logging runs first — wraps entire pipeline
        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(LoggingBehavior<,>));

        // 2. Validation runs second — before handler
        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationBehavior<,>));

        return services;
    }
}