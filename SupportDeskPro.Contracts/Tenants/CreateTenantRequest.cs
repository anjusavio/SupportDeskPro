namespace SupportDeskPro.Contracts.Tenants;

// Request model for creating a new tenant via SuperAdmin panel
public record CreateTenantRequest(
    string Name,
    string Slug,
    int PlanType,
    int MaxAgents,
    int MaxTickets
);