/// <summary>
/// Response model for Admin dashboard — tenant-wide ticket statistics,
/// SLA performance, agent workload and category breakdown.
/// </summary>
namespace SupportDeskPro.Contracts.Dashboard;

public record AdminDashboardResponse(
    // Ticket counts
    int TotalTickets,
    int OpenTickets,
    int InProgressTickets,
    int ResolvedTickets,
    int ClosedTickets,

    // Today stats
    int TicketsCreatedToday,
    int TicketsResolvedToday,

    // SLA stats
    int SLABreachedCount,
    int SLABreachedToday,

    // Performance
    double AverageResolutionTimeHours,

    // Breakdowns
    List<CategoryTicketCount> TicketsByCategory,
    List<AgentTicketCount> TicketsByAgent,
    List<PriorityTicketCount> TicketsByPriority
);

public record CategoryTicketCount(
    string CategoryName,
    int OpenCount,
    int TotalCount
);

public record AgentTicketCount(
    string AgentName,
    int OpenCount,
    int InProgressCount,
    int ResolvedTodayCount
);

public record PriorityTicketCount(
    string Priority,
    int Count
);