/// <summary>
/// Response model for Agent dashboard — personal ticket statistics
/// and SLA performance for the currently authenticated agent.
/// </summary>
namespace SupportDeskPro.Contracts.Dashboard;

public record AgentDashboardResponse(
    // My ticket counts
    int MyTotalAssigned,
    int MyOpenTickets,
    int MyInProgressTickets,
    int MyResolvedToday,

    // My SLA stats
    int MySLABreachedCount,
    int MySLAPendingCount,

    // My performance
    double MyAverageResolutionTimeHours,

    // My recent tickets
    List<RecentTicketSummary> MyRecentTickets
);

public record RecentTicketSummary(
    Guid Id,
    int TicketNumber,
    string Title,
    string Status,
    string Priority,
    bool IsSLABreached,
    DateTime CreatedAt
);