namespace SupportDeskPro.Application.Common;

/// <summary>
/// Centralised cache key constants.
/// Prevents typos and makes cache invalidation easier to find.
/// </summary>
public static class CacheKeys
{
    // Categories — cache for 1 hour, invalidate on create/update
    public static string ActiveCategories(Guid tenantId)
        => $"categories:active:{tenantId}";//Key becomes:"categories:active:abc123-4567-89ab-cdef-000000000000"

    // SLA Policies — cache for 1 hour, invalidate on create/update
    public static string SLAPolicies(Guid tenantId)
        => $"sla-policies:{tenantId}";

    // Agents list — cache for 5 minutes (workload count changes often)
    public static string Agents(Guid tenantId)
        => $"agents:{tenantId}";

    // Similar tickets — cache for 1 hour per ticket (AI call)
    public static string SimilarTickets(Guid ticketId)
        => $"similar-tickets:{ticketId}";

    // Dashboard stats — cache for 5 minutes
    public static string AdminDashboard(Guid tenantId)
        => $"dashboard:admin:{tenantId}";

    public static string AgentDashboard(Guid agentId)
        => $"dashboard:agent:{agentId}";

    // Tenant settings — cache for 30 minutes
    public static string TenantSettings(Guid tenantId)
        => $"tenant-settings:{tenantId}";
}