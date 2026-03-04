/// <summary>
/// Command model for Customer creating a new support ticket.
/// Triggers SLA assignment and ticket number generation.
/// </summary>
using MediatR;

namespace SupportDeskPro.Application.Features.Tickets.CreateTicket;

public record CreateTicketCommand(
    Guid TenantId,
    Guid CustomerId,
    string Title,
    string Description,
    Guid CategoryId,
    int Priority
) : IRequest<CreateTicketResult>;

public record CreateTicketResult(
    bool Success,
    string Message,
    Guid? TicketId = null,
    int? TicketNumber = null
);