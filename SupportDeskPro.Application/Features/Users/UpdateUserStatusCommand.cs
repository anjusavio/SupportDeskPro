// Command model for activating or deactivating any user (Admin only)
using MediatR;

namespace SupportDeskPro.Application.Features.Users.UpdateUserStatus;

public record UpdateUserStatusCommand(
    Guid UserId,
    bool IsActive
) : IRequest<UpdateUserStatusResult>;

public record UpdateUserStatusResult(
    bool Success,
    string Message
);