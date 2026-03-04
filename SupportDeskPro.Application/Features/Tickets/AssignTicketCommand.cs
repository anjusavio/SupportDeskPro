/// <summary>
/// Command model for Admin to assign or unassign a ticket to an agent.
/// Null AgentId means unassign the ticket.
/// Assignment logged to TicketAssignmentHistory automatically.
/// </summary>
using MediatR;

namespace SupportDeskPro.Application.Features.Tickets.AssignTicket;

public record AssignTicketCommand(
    Guid TicketId,
    Guid AssignedById,
    Guid? AgentId
) : IRequest<AssignTicketResult>;

public record AssignTicketResult(
    bool Success,
    string Message
);