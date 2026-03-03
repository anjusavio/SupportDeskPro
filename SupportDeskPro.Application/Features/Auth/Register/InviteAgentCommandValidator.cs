/// <summary>
/// FluentValidation validator for InviteAgentCommand.
/// Validates agent invitation request before handler creates the user.
/// </summary>
using FluentValidation;

namespace SupportDeskPro.Application.Features.Users.InviteAgent;

public class InviteAgentCommandValidator
    : AbstractValidator<InviteAgentCommand>
{
    public InviteAgentCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(320).WithMessage("Email cannot exceed 320 characters.");
    }
}