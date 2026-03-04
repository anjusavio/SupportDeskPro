/// <summary>
/// FluentValidation rules for CreateCommentCommand.
/// Validates body is not empty and within length limit.
/// </summary>
using FluentValidation;

namespace SupportDeskPro.Application.Features.Comments.CreateComment;

public class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentCommandValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Comment body is required.")
            .MaximumLength(4000)
            .WithMessage("Comment cannot exceed 4000 characters.");
    }
}