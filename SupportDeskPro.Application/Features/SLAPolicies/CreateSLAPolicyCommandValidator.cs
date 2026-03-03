/// <summary>
/// FluentValidation rules for CreateSLAPolicyCommand.
/// Validates time values are positive and resolution exceeds first response.
/// </summary>
using FluentValidation;

namespace SupportDeskPro.Application.Features.SLAPolicies.CreateSLAPolicy;

public class CreateSLAPolicyCommandValidator : AbstractValidator<CreateSLAPolicyCommand>
{
    public CreateSLAPolicyCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Policy name is required.")
            .MaximumLength(100).WithMessage("Policy name cannot exceed 100 characters.");

        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 4)
            .WithMessage("Priority must be 1=Low, 2=Medium, 3=High, 4=Critical.");

        RuleFor(x => x.FirstResponseTimeMinutes)
            .GreaterThan(0)
            .WithMessage("First response time must be greater than 0.");

        RuleFor(x => x.ResolutionTimeMinutes)
            .GreaterThan(0)
            .WithMessage("Resolution time must be greater than 0.")
            .GreaterThan(x => x.FirstResponseTimeMinutes)
            .WithMessage("Resolution time must be greater than first response time.");
    }
}