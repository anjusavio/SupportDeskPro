/// <summary>
/// Handles paginated ticket list for Admin and Agent.
/// Supports filtering by status, priority, category, agent and SLA breach.
/// Scoped to current tenant via Global Query Filter.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Features.Tenants.GetTenants;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Tickets;
using SupportDeskPro.Domain.Enums;

namespace SupportDeskPro.Application.Features.Tickets.GetTickets;

public class GetTicketsQueryHandler
    : IRequestHandler<GetTicketsQuery, PagedResult<TicketResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;

    public GetTicketsQueryHandler(IApplicationDbContext db, ICurrentTenantService currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<PagedResult<TicketResponse>> Handle(
        GetTicketsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Tickets
            .Include(t => t.Category)
            .Include(t => t.Customer)
            .Include(t => t.AssignedAgent)
            .Where(t => !t.IsDeleted)
            .AsQueryable();

        // Agent sees only their assigned tickets
        if (_currentTenant.CurrentUserRole == "Agent")
        {
            query = query.Where(t =>
                t.AssignedAgentId == _currentTenant.CurrentUserId);
        }

        // Apply filters
        if (request.Status.HasValue)
            query = query.Where(t =>
                (int)t.Status == request.Status.Value);

        if (request.Priority.HasValue)
            query = query.Where(t =>
                (int)t.Priority == request.Priority.Value);

        if (request.CategoryId.HasValue)
            query = query.Where(t =>
                t.CategoryId == request.CategoryId);

        if (request.AssignedAgentId.HasValue)
            query = query.Where(t =>
                t.AssignedAgentId == request.AssignedAgentId);

        if (request.IsSLABreached.HasValue)
            query = query.Where(t =>
                t.IsSLABreached == request.IsSLABreached);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(t =>
                t.Title.ToLower().Contains(search) ||
                t.Description.ToLower().Contains(search) ||
                t.Customer.FirstName.ToLower().Contains(search) ||
                t.Customer.LastName.ToLower().Contains(search));
        }

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