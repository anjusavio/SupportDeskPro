namespace SupportDeskPro.Contracts.Dashboard;

/// <summary>
/// SuperAdmin dashboard response — platform-wide statistics.
/// Shows overall health of the platform and per-tenant breakdown.
/// SuperAdmin sees all tenants — no tenant scoping applied.
/// </summary>
public record SuperAdminDashboardResponse(
    // Platform overview cards
    int TotalTenants,
    int ActiveTenants,
    int InactiveTenants,
    int TotalUsers,
    int TotalAgents,
    int TotalTickets,
    int TicketsToday,
    int TotalEmailsSent,
    int EmailFailures,
    decimal PlatformSLAComplianceRate,  // % of tickets meeting SLA across all tenants

    // Per-tenant breakdown — sorted by ticket volume descending
    List<TenantActivityResponse> TenantActivity,

    // Recent tenant registrations
    List<RecentTenantResponse> RecentTenants
);

/// <summary>
/// Per-tenant stats shown in the activity table.
/// Includes SLA health indicator to identify struggling tenants.
/// </summary>
public record TenantActivityResponse(
    Guid TenantId,
    string TenantName,
    string Slug,
    int TotalTickets,
    int OpenTickets,
    int ResolvedTickets,
    int TotalAgents,
    int TotalCustomers,
    decimal SLAComplianceRate,  // % of tickets not breached
    string SLAHealth,           // Good | AtRisk | Poor
    bool IsActive,
    DateTime CreatedAt
);

/// <summary>
/// Recently registered tenants shown at bottom of dashboard.
/// Tracks platform growth over time.
/// </summary>
public record RecentTenantResponse(
    Guid TenantId,
    string TenantName,
    string Slug,
    int PlanType,
    DateTime CreatedAt
);