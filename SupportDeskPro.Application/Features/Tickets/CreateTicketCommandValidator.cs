/// <summary>
/// FluentValidation rules for CreateTicketCommand.
/// Validates title length, description and priority range.
/// </summary>
using FluentValidation;

namespace SupportDeskPro.Application.Features.Tickets.CreateTicket;

public class CreateTicketCommandValidator
    : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(4000).WithMessage("Description cannot exceed 4000 characters.");

        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 4)
            .WithMessage("Priority must be 1=Low, 2=Medium, 3=High, 4=Critical.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");
    }
}