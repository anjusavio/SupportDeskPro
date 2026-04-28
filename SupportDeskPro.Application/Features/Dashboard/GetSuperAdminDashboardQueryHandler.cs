using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Dashboard;
using SupportDeskPro.Domain.Enums;

namespace SupportDeskPro.Application.Features.Dashboard.GetSuperAdminDashboard;

/// <summary>
/// Handles SuperAdmin dashboard data aggregation.
///
/// Queries run sequentially — EF Core DbContext is not thread safe.
/// Task.WhenAll cannot be used with a single DbContext instance.
///
/// No tenant scoping applied — SuperAdmin sees all data across
/// every tenant on the platform simultaneously.
///
/// SLA health classification per tenant:
///   Good   = 90%+ compliance  — tenant performing well
///   AtRisk = 70-89% compliance — tenant needs attention
///   Poor   = below 70%         — tenant requires intervention
///
/// Results cached for 5 minutes — cross-tenant aggregation queries
/// are expensive and do not need to run on every page refresh.
/// </summary>
public class GetSuperAdminDashboardQueryHandler
    : IRequestHandler<GetSuperAdminDashboardQuery, SuperAdminDashboardResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GetSuperAdminDashboardQueryHandler> _logger;

    public GetSuperAdminDashboardQueryHandler(
        IApplicationDbContext db,
        IMemoryCache cache,
        ILogger<GetSuperAdminDashboardQueryHandler> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<SuperAdminDashboardResponse> Handle(
        GetSuperAdminDashboardQuery request,
        CancellationToken cancellationToken)
    {
        const string cacheKey = "dashboard:superadmin";

        // Return cached result if available — avoids repeated DB hits
        if (_cache.TryGetValue(cacheKey, out SuperAdminDashboardResponse? cached))
            return cached!;

        var now = DateTime.UtcNow;
        var todayStart = now.Date;

        // ── 1. Tenants ────────────────────────────────────────────────────
        var tenants = await _db.Tenants
            .Where(t => !t.IsDeleted)
            .ToListAsync(cancellationToken);

        // ── 2. Users grouped by role ──────────────────────────────────────
        var userGroups = await _db.Users
            .Where(u => !u.IsDeleted)
            .GroupBy(u => u.Role)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // ── 3. Ticket counts ──────────────────────────────────────────────
        var totalTickets = await _db.Tickets
            .Where(t => !t.IsDeleted)
            .CountAsync(cancellationToken);

        var ticketsToday = await _db.Tickets
            .Where(t => !t.IsDeleted && t.CreatedAt >= todayStart)
            .CountAsync(cancellationToken);

        var slaBreached = await _db.Tickets
            .Where(t => !t.IsDeleted && t.IsSLABreached)
            .CountAsync(cancellationToken);

        // ── 4. Email stats — optional, fail silently ──────────────────────
        // Email table may have issues — dashboard should not crash for this
        int emailSent = 0;
        int emailFailed = 0;

        try
        {
            emailSent = await _db.EmailLogs
                .Where(e => e.Status == EmailStatus.Sent)
                .CountAsync(cancellationToken);

            emailFailed = await _db.EmailLogs
                .Where(e => e.Status == EmailStatus.Failed)
                .CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to load email stats for SuperAdmin dashboard. " +
                "Showing zeros.");
        }

        // ── 5. Per-tenant ticket breakdown ────────────────────────────────
        var ticketsByTenant = await _db.Tickets
            .Where(t => !t.IsDeleted)
            .GroupBy(t => t.TenantId)
            .Select(g => new
            {
                TenantId = g.Key,
                Total = g.Count(),
                Open = g.Count(t =>
                                t.Status == TicketStatus.Open ||
                                t.Status == TicketStatus.InProgress ||
                                t.Status == TicketStatus.OnHold),
                Resolved = g.Count(t =>
                                t.Status == TicketStatus.Resolved ||
                                t.Status == TicketStatus.Closed),
                SLABreached = g.Count(t => t.IsSLABreached)
            })
            .ToListAsync(cancellationToken);

        // ── 6. Per-tenant user breakdown ──────────────────────────────────
        var usersByTenant = await _db.Users
            .Where(u => !u.IsDeleted)
            .GroupBy(u => u.TenantId)
            .Select(g => new
            {
                TenantId = g.Key,
                Agents = g.Count(u => u.Role == UserRole.Agent),
                Customers = g.Count(u => u.Role == UserRole.Customer)
            })
            .ToListAsync(cancellationToken);

        // ── 7. Calculate platform SLA compliance rate ─────────────────────
        var slaCompliance = totalTickets > 0
            ? Math.Round(
                (decimal)(totalTickets - slaBreached) / totalTickets * 100, 1)
            : 100m;

        // ── 8. Build per-tenant activity list ────────────────────────────
        var tenantActivity = tenants
            .Select(t =>
            {
                var tickets = ticketsByTenant
                    .FirstOrDefault(x => x.TenantId == t.Id);
                var users = usersByTenant
                    .FirstOrDefault(x => x.TenantId == t.Id);

                var total = tickets?.Total ?? 0;
                var breached = tickets?.SLABreached ?? 0;
                var compliance = total > 0
                    ? Math.Round(
                        (decimal)(total - breached) / total * 100, 1)
                    : 100m;

                // Visual health indicator for dashboard table
                var slaHealth = compliance >= 90 ? "Good"
                              : compliance >= 70 ? "AtRisk"
                              : "Poor";

                return new TenantActivityResponse(
                    t.Id,
                    t.Name,
                    t.Slug,
                    total,
                    tickets?.Open ?? 0,
                    tickets?.Resolved ?? 0,
                    users?.Agents ?? 0,
                    users?.Customers ?? 0,
                    compliance,
                    slaHealth,
                    t.IsActive,
                    t.CreatedAt);
            })
            .OrderByDescending(t => t.TotalTickets) // most active first
            .ToList();

        // ── 9. Recent tenant registrations ───────────────────────────────
        var recentTenants = tenants
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .Select(t => new RecentTenantResponse(
                t.Id,
                t.Name,
                t.Slug,
                (int)t.PlanType,
                t.CreatedAt))
            .ToList();

        // ── 10. Aggregate user counts ─────────────────────────────────────
        var totalUsers = userGroups.Sum(g => g.Count);
        var totalAgents = userGroups
            .FirstOrDefault(g => g.Role == UserRole.Agent)?.Count ?? 0;

        // ── 11. Build final response ──────────────────────────────────────
        var response = new SuperAdminDashboardResponse(
            TotalTenants: tenants.Count,
            ActiveTenants: tenants.Count(t => t.IsActive),
            InactiveTenants: tenants.Count(t => !t.IsActive),
            TotalUsers: totalUsers,
            TotalAgents: totalAgents,
            TotalTickets: totalTickets,
            TicketsToday: ticketsToday,
            TotalEmailsSent: emailSent,
            EmailFailures: emailFailed,
            PlatformSLAComplianceRate: slaCompliance,
            TenantActivity: tenantActivity,
            RecentTenants: recentTenants);

        // Cache for 5 minutes
        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));

        return response;
    }
}