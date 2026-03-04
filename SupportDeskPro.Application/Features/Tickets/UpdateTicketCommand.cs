/// <summary>
/// Command model for Admin/Agent to update ticket details.
/// </summary>
using MediatR;

namespace SupportDeskPro.Application.Features.Tickets.UpdateTicket;

public record UpdateTicketCommand(
    Guid TicketId,
    string Title,
    string Description,
    Guid CategoryId,
    int Priority
) : IRequest<UpdateTicketResult>;

public record UpdateTicketResult(
    bool Success,
    string Message
);