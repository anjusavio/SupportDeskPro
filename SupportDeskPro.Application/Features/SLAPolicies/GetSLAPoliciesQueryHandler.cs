/// <summary>
/// Handles fetching paginated SLA policy list ordered by priority.
/// Scoped to current tenant via Global Query Filter.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Features.Tenants.GetTenants;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.SLAPolicies;

namespace SupportDeskPro.Application.Features.SLAPolicies.GetSLAPolicies;

public class GetSLAPoliciesQueryHandler
    : IRequestHandler<GetSLAPoliciesQuery, PagedResult<SLAPolicyResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetSLAPoliciesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<SLAPolicyResponse>> Handle(
        GetSLAPoliciesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.SLAPolicies.AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(s => s.IsActive == request.IsActive);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.Priority)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new SLAPolicyResponse(
                s.Id,
                s.Name,
                s.Priority.ToString(),
                s.FirstResponseTimeMinutes,
                s.ResolutionTimeMinutes,
                s.IsActive,
                s.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<SLAPolicyResponse>(
            items, totalCount, request.Page, request.PageSize);
    }
}