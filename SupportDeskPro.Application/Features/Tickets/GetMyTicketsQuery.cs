/// <summary>
/// Query model for Customer to view their own tickets only.
/// </summary>
using MediatR;
using SupportDeskPro.Application.Features.Tenants.GetTenants;
using SupportDeskPro.Contracts.Tickets;

namespace SupportDeskPro.Application.Features.Tickets.GetMyTickets;

public record GetMyTicketsQuery(
    Guid CustomerId,
    int Page = 1,
    int PageSize = 20,
    int? Status = null
) : IRequest<PagedResult<TicketResponse>>;