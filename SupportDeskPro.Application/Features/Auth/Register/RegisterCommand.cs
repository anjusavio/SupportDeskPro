using MediatR;
using SupportDeskPro.Contracts.Auth;

namespace SupportDeskPro.Application.Features.Auth.Register
{
    public record RegisterCommand(
     string FirstName,
     string LastName,
     string Email,
     string Password,
     string ConfirmPassword
 ) : IRequest<RegisterResult>;

    public record RegisterResult(bool Success, string Message);
}
