// Handles fetching active agents — used in ticket assignment dropdown
//giving suggestion for least busy agent based on open ticket count.
// so admin can evenly distribute workload among agents.
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Users;
using SupportDeskPro.Domain.Enums;

namespace SupportDeskPro.Application.Features.Users.GetAgents;

public class GetAgentsQueryHandler
    : IRequestHandler<GetAgentsQuery, List<AgentSummaryResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetAgentsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<AgentSummaryResponse>> Handle(
        GetAgentsQuery request,
        CancellationToken cancellationToken)
    {
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

        return result.Select(a => new AgentSummaryResponse(
              a.Agent.Id,
              a.Agent.FirstName,
              a.Agent.LastName,
              a.Agent.Email,
              a.OpenTicketCount,
              IsRecommended: a.OpenTicketCount == minCount 
          )).ToList();
    }
}