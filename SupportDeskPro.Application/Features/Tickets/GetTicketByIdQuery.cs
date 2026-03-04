/// <summary>
/// Query model for retrieving full ticket detail including AI suggestions.
/// </summary>
using MediatR;
using SupportDeskPro.Contracts.Tickets;

namespace SupportDeskPro.Application.Features.Tickets.GetTicketById;

public record GetTicketByIdQuery(Guid TicketId)
    : IRequest<TicketDetailResponse>;