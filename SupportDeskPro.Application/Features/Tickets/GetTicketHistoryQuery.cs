/// <summary>
/// Query model for retrieving full status change history for a ticket.
/// </summary>
using MediatR;
using SupportDeskPro.Contracts.Tickets;

namespace SupportDeskPro.Application.Features.Tickets.GetTicketHistory;

public record GetTicketHistoryQuery(Guid TicketId)
    : IRequest<List<TicketStatusHistoryResponse>>;