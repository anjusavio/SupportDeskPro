// Command model for changing a user role between Agent and Customer (Admin only)
using MediatR;

namespace SupportDeskPro.Application.Features.Users.UpdateUserRole;

public record UpdateUserRoleCommand(
    Guid UserId,
    int Role
) : IRequest<UpdateUserRoleResult>;

public record UpdateUserRoleResult(
    bool Success,
    string Message
);