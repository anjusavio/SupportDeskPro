/// <summary>
/// Handles Agent dashboard data aggregation.
/// Returns personal performance stats for the authenticated agent.
/// Scoped to current tenant via Global Query Filter.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Dashboard;
using SupportDeskPro.Domain.Entities;
using SupportDeskPro.Domain.Enums;

namespace SupportDeskPro.Application.Features.Dashboard.GetAgentDashboard;

public class GetAgentDashboardQueryHandler
    : IRequestHandler<GetAgentDashboardQuery, AgentDashboardResponse>
{
    private readonly IApplicationDbContext _db;

    public GetAgentDashboardQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    //Agent Dashboard:
    //-> My assigned tickets by status
    //-> My SLA breaches
    //-> My resolved today count
    //-> My average resolution time

    public async Task<AgentDashboardResponse> Handle(
        GetAgentDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var todayEnd = todayStart.AddDays(1);

        // 1. Base query — my assigned tickets only
        var myTickets = _db.Tickets.Where(t => t.AssignedAgentId == request.AgentId
                                            && !t.IsDeleted);

        // 2. My ticket counts
        var myTotalAssigned = await myTickets.CountAsync(cancellationToken);

        var myOpenTickets = await myTickets.CountAsync(t => t.Status == TicketStatus.Open,cancellationToken);

        var myInProgressTickets = await myTickets.CountAsync(t => t.Status == TicketStatus.InProgress, cancellationToken);

        var myResolvedToday = await myTickets.CountAsync(t => t.ResolvedAt >= todayStart
                                     && t.ResolvedAt < todayEnd,cancellationToken);

        // 3. My SLA stats
        var mySLABreachedCount = await myTickets.CountAsync(t => t.IsSLABreached, cancellationToken);

        // Tickets approaching SLA breach — due in next 2 hours
        var mySLAPendingCount = await myTickets
            .CountAsync(t => !t.IsSLABreached
                             && t.SLAResolutionDueAt != null
                             && t.SLAResolutionDueAt <= now.AddHours(2)
                             && t.Status != TicketStatus.Resolved
                             && t.Status != TicketStatus.Closed,
                cancellationToken);

        // 4. My average resolution time
        var myResolvedWithTime = await myTickets
                .Where(t => t.ResolvedAt != null)
                .Select(t => new
                {
                    CreatedAt = t.CreatedAt,
                    ResolvedAt = t.ResolvedAt!.Value
                })
                .ToListAsync(cancellationToken);

        var myAvgResolutionHours = myResolvedWithTime.Any()
            ? myResolvedWithTime
                .Average(t => (t.ResolvedAt - t.CreatedAt).TotalHours)
            : 0;

        // 5. My recent 10 tickets
        var myRecentTickets = await myTickets
            .OrderByDescending(t => t.LastActivityAt)
            .Take(10)
            .Select(t => new RecentTicketSummary(
                t.Id,
                t.TicketNumber,
                t.Title,
                t.Status.ToString(),
                t.Priority.ToString(),
                t.IsSLABreached,
                t.CreatedAt))
            .ToListAsync(cancellationToken);

        return new AgentDashboardResponse(
            myTotalAssigned,
            myOpenTickets,
            myInProgressTickets,
            myResolvedToday,
            mySLABreachedCount,
            mySLAPendingCount,
            Math.Round(myAvgResolutionHours, 1),
            myRecentTickets);
    }
}