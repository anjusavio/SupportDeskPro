// Handles fetching active agents — used in ticket assignment dropdown
//giving suggestion for least busy agent based on open ticket count.
// so admin can evenly distribute workload among agents.
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SupportDeskPro.Application.Common;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Users;
using SupportDeskPro.Domain.Enums;

namespace SupportDeskPro.Application.Features.Users.GetAgents;

public class GetAgentsQueryHandler
    : IRequestHandler<GetAgentsQuery, List<AgentSummaryResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ICurrentTenantService _currentTenant;

    public GetAgentsQueryHandler(IApplicationDbContext db, IMemoryCache cache, ICurrentTenantService currentTenant)
    {
        _db = db;
        _cache = cache;
        _currentTenant = currentTenant;
    }

    public async Task<List<AgentSummaryResponse>> Handle(
        GetAgentsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var cacheKey = CacheKeys.Agents(tenantId);

        if (_cache.TryGetValue(cacheKey, out List<AgentSummaryResponse>? cached))
            return cached!;


        // Get all active agents in tenant
        var agents = await _db.Users
            .Where(u => u.Role == UserRole.Agent && u.IsActive && !u.IsDeleted)
            .ToListAsync(cancellationToken);

        // Count unresolved tickets per agent
        //    Unresolved = Open, InProgress, OnHold
        var workload = await _db.Tickets
            .Where(t => !t.IsDeleted
                        && t.AssignedAgentId != null
                        && t.Status != TicketStatus.Resolved
                        && t.Status != TicketStatus.Closed)
            .GroupBy(t => t.AssignedAgentId)
            .Select(g => new
            {
                AgentId = g.Key,
                OpenTicketCount = g.Count()
            })
            .ToListAsync(cancellationToken);

        //  Map workload to agents
        var result = agents.Select(a => new
        {
            Agent = a,
            OpenTicketCount = workload
                .FirstOrDefault(w => w.AgentId == a.Id)
                ?.OpenTicketCount ?? 0
        })
        .OrderBy(a => a.OpenTicketCount) // least busy agent listing on top of the list
        .ToList();

        //  Find minimum count for recommendation
        var minCount = result.Any() ? result.Min(a => a.OpenTicketCount) : 0;

        var response= result.Select(a => new AgentSummaryResponse(
              a.Agent.Id,
              a.Agent.FirstName,
              a.Agent.LastName,
              a.Agent.Email,
              a.OpenTicketCount,
              IsRecommended: a.OpenTicketCount == minCount 
          )).ToList();

        // Cache for 5 minutes — workload changes often
        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));

        return response;
    }
}