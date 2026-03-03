// Command model for Admin to invite a new agent — sends welcome email with temp password
using MediatR;

namespace SupportDeskPro.Application.Features.Users.InviteAgent;

public record InviteAgentCommand(
    Guid TenantId,
    string FirstName,
    string LastName,
    string Email
) : IRequest<InviteAgentResult>;

public record InviteAgentResult(
    bool Success,
    string Message,
    Guid? UserId = null
);