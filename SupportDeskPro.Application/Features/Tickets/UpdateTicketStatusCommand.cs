/// <summary>
/// Command model for Admin/Agent to change ticket status.
/// Status changes are logged to TicketStatusHistory automatically.
/// </summary>
using MediatR;

namespace SupportDeskPro.Application.Features.Tickets.UpdateTicketStatus;

public record UpdateTicketStatusCommand(
    Guid TicketId,
    Guid ChangedById,
    int Status,
    string? Note
) : IRequest<UpdateTicketStatusResult>;

public record UpdateTicketStatusResult(
    bool Success,
    string Message
);