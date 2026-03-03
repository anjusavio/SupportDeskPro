using MediatR;
using SupportDeskPro.Contracts.Auth;

namespace SupportDeskPro.Application.Features.Auth.Register
{
    public record RegisterCommand(
     string FirstName,
     string LastName,
     string Email,
     string Password,
     string ConfirmPassword,
     string TenantSlug //— which company they belong to
 ) : IRequest<RegisterResult>;

    public record RegisterResult(bool Success, string Message);
}
