namespace SupportDeskPro.Contracts.Tenants;

// Request model for updating tenant name, plan, and limits
public record UpdateTenantRequest(
    string Name,
    int PlanType,
    int MaxAgents,
    int MaxTickets
);