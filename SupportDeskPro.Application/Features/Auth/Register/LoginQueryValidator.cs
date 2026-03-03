/// <summary>
/// FluentValidation validator for LoginQuery.
/// Basic format validation before hitting the database.
/// Prevents unnecessary DB calls for obviously invalid requests.
/// </summary>
using FluentValidation;

namespace SupportDeskPro.Application.Features.Auth.Login;

public class LoginQueryValidator
    : AbstractValidator<LoginQuery>
{
    public LoginQueryValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}