/// <summary>
/// Handles Admin dashboard data aggregation.
/// Runs multiple lightweight queries and assembles
/// a single comprehensive dashboard response.
/// All queries scoped to current tenant via Global Query Filter.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SupportDeskPro.Application.Common;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Dashboard;
using SupportDeskPro.Domain.Enums;

namespace SupportDeskPro.Application.Features.Dashboard.GetAdminDashboard;

public class GetAdminDashboardQueryHandler
    : IRequestHandler<GetAdminDashboardQuery, AdminDashboardResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ICurrentTenantService _currentTenant;

    public GetAdminDashboardQueryHandler(
        IApplicationDbContext db,
        IMemoryCache cache,
        ICurrentTenantService currentTenant)
    {
        _db = db;
        _cache = cache;
        _currentTenant = currentTenant;
    }

    public async Task<AdminDashboardResponse> Handle(
        GetAdminDashboardQuery request,
        CancellationToken cancellationToken)
    {
        //cache dashboard data for 5 minutes — expensive to calculate on every page load
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var cacheKey = CacheKeys.AdminDashboard(tenantId);

        if (_cache.TryGetValue(cacheKey, out AdminDashboardResponse? cached))
            return cached!;

        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var todayEnd = todayStart.AddDays(1);

        // 1. Base query — active tickets only
        var tickets = _db.Tickets
            .Where(t => !t.IsDeleted);

        // 2. Ticket counts by status
        var totalTickets = await tickets
            .CountAsync(cancellationToken);

        var openTickets = await tickets
            .CountAsync(t => t.Status == TicketStatus.Open,
                cancellationToken);

        var inProgressTickets = await tickets
            .CountAsync(t => t.Status == TicketStatus.InProgress,
                cancellationToken);

        var resolvedTickets = await tickets
            .CountAsync(t => t.Status == TicketStatus.Resolved,
                cancellationToken);

        var closedTickets = await tickets
            .CountAsync(t => t.Status == TicketStatus.Closed,
                cancellationToken);

        // 3. Today stats
        var ticketsCreatedToday = await tickets
            .CountAsync(t => t.CreatedAt >= todayStart
                             && t.CreatedAt < todayEnd,
                cancellationToken);

        var ticketsResolvedToday = await tickets
            .CountAsync(t => t.ResolvedAt >= todayStart
                             && t.ResolvedAt < todayEnd,
                cancellationToken);

        // 4. SLA stats
        var slaBreachedCount = await tickets
            .CountAsync(t => t.IsSLABreached, cancellationToken);

        var slaBreachedToday = await tickets
            .CountAsync(t => t.IsSLABreached
                             && t.SLABreachedAt >= todayStart
                             && t.SLABreachedAt < todayEnd,
                cancellationToken);

        // 5. Average resolution time
        // Fetch raw dates first — C# calculates TimeSpan after ToListAsync
        var resolvedWithTime = await tickets
            .Where(t => t.ResolvedAt != null)
            .Select(t => new
            {
                t.CreatedAt,
                ResolvedAt = t.ResolvedAt!.Value
            })
            .ToListAsync(cancellationToken);

        var avgResolutionHours = resolvedWithTime.Any()
            ? resolvedWithTime
                .Average(t => (t.ResolvedAt - t.CreatedAt).TotalHours)
            : 0;

        // 6. Tickets by category
        // Fetch raw data first — GroupBy in C# memory
        var rawCategoryData = await tickets
            .Include(t => t.Category)
            .Select(t => new
            {
                CategoryName = t.Category.Name,
                t.Status
            })
            .ToListAsync(cancellationToken);

        var ticketsByCategory = rawCategoryData
            .GroupBy(t => t.CategoryName)
            .Select(g => new CategoryTicketCount(
                g.Key,
                g.Count(t => t.Status == TicketStatus.Open),
                g.Count()))
            .OrderByDescending(c => c.TotalCount)
            .Take(10)
            .ToList();

        // 7. Tickets by agent
        // Fetch raw data first — GroupBy in C# memory
        var rawAgentData = await tickets
            .Where(t => t.AssignedAgentId != null)
            .Include(t => t.AssignedAgent)
            .Select(t => new
            {
                AgentName = t.AssignedAgent!.FirstName
                            + " " + t.AssignedAgent.LastName,
                t.Status,
                t.ResolvedAt
            })
            .ToListAsync(cancellationToken);

        var ticketsByAgent = rawAgentData
            .GroupBy(t => t.AgentName)
            .Select(g => new AgentTicketCount(
                g.Key,
                g.Count(t => t.Status == TicketStatus.Open),
                g.Count(t => t.Status == TicketStatus.InProgress),
                g.Count(t => t.ResolvedAt >= todayStart
                             && t.ResolvedAt < todayEnd)))
            .OrderByDescending(a => a.OpenCount + a.InProgressCount)
            .ToList();

        // 8. Tickets by priority
        // Fetch raw data first — GroupBy in C# memory
        var rawPriorityData = await tickets
            .Select(t => new { t.Priority })
            .ToListAsync(cancellationToken);

        var ticketsByPriority = rawPriorityData
            .GroupBy(t => t.Priority)
            .Select(g => new PriorityTicketCount(
                g.Key.ToString(),
                g.Count()))
            .ToList();

        var response = new AdminDashboardResponse(
            totalTickets,
            openTickets,
            inProgressTickets,
            resolvedTickets,
            closedTickets,
            ticketsCreatedToday,
            ticketsResolvedToday,
            slaBreachedCount,
            slaBreachedToday,
            Math.Round(avgResolutionHours, 1),
            ticketsByCategory,
            ticketsByAgent,
            ticketsByPriority);

        // Cache for 5 minutes
        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));

        return response;
    }
}