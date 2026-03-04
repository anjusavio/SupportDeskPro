/// <summary>
/// Query model for Admin/Agent paginated ticket list
/// with filtering by status, priority, category and agent.
/// </summary>
using MediatR;
using SupportDeskPro.Application.Features.Tenants.GetTenants;
using SupportDeskPro.Contracts.Tickets;

namespace SupportDeskPro.Application.Features.Tickets.GetTickets;

public record GetTicketsQuery(
    int Page = 1,
    int PageSize = 20,
    int? Status = null,
    int? Priority = null,
    Guid? CategoryId = null,
    Guid? AssignedAgentId = null,
    bool? IsSLABreached = null,
    string? Search = null
) : IRequest<PagedResult<TicketResponse>>;