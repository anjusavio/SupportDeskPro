using MediatR;
using SupportDeskPro.Contracts.Tickets;

namespace SupportDeskPro.Application.Features.Tickets.AIGetSimilarTickets;

public record AIGetSimilarTicketsQuery(Guid TicketId)
    : IRequest<List<AISimilarTicketResponse>>;