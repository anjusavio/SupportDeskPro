using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Tenants;

namespace SupportDeskPro.Application.Features.Tenants.GetTenants;

// Handles fetching paginated tenant list with optional active status filter
public class GetTenantsQueryHandler : IRequestHandler<GetTenantsQuery, PagedResult<TenantResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetTenantsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<TenantResponse>> Handle(
        GetTenantsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Tenants.AsQueryable();

        // Filter by status if provided
        if (request.IsActive.HasValue)
            query = query.Where(t => t.IsActive == request.IsActive);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TenantResponse(
                t.Id,
                t.Name,
                t.Slug,
                t.PlanType.ToString(),
                t.IsActive,
                t.MaxAgents,
                t.MaxTickets,
                t.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<TenantResponse>(
            items, totalCount, request.Page, request.PageSize);
    }
}