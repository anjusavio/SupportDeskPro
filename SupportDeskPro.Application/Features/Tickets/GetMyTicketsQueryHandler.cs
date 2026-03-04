/// <summary>
/// Handles fetching tickets for the currently logged in Customer.
/// Filters by CustomerId ensuring customers only see their own tickets.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Features.Tenants.GetTenants;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Tickets;

namespace SupportDeskPro.Application.Features.Tickets.GetMyTickets;

public class GetMyTicketsQueryHandler
    : IRequestHandler<GetMyTicketsQuery, PagedResult<TicketResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetMyTicketsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<TicketResponse>> Handle(
        GetMyTicketsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Tickets
            .Include(t => t.Category)
            .Include(t => t.Customer)
            .Include(t => t.AssignedAgent)
            .Where(t => t.CustomerId == request.CustomerId
                        && !t.IsDeleted)
            .AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(t =>
                (int)t.Status == request.Status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TicketResponse(
                t.Id,
                t.TicketNumber,
                t.Title,
                t.Description,
                t.Status.ToString(),
                t.Priority.ToString(),
                t.CategoryId,
                t.Category.Name,
                t.CustomerId,
                t.Customer.FirstName + " " + t.Customer.LastName,
                t.AssignedAgentId,
                t.AssignedAgent != null
                    ? t.AssignedAgent.FirstName + " " + t.AssignedAgent.LastName
                    : null,
                t.SLAFirstResponseDueAt,
                t.SLAResolutionDueAt,
                t.FirstResponseAt,
                t.ResolvedAt,
                t.IsSLABreached,
                t.LastActivityAt,
                t.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<TicketResponse>(
            items, totalCount, request.Page, request.PageSize);
    }
}