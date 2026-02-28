namespace SupportDeskPro.Contracts.Tenants;

// Response models for tenant list, detail, and settings views
public record TenantResponse(
    Guid Id,
    string Name,
    string Slug,
    string PlanType,
    bool IsActive,
    int MaxAgents,
    int MaxTickets,
    DateTime CreatedAt
);

public record TenantSettingsResponse(
    Guid TenantId,
    string TimeZone,
    string WorkingHoursStart,
    string WorkingHoursEnd,
    string WorkingDays,
    int AutoCloseAfterDays,
    bool AllowCustomerSelfRegistration
);

public record TenantDetailResponse(
    Guid Id,
    string Name,
    string Slug,
    string PlanType,
    bool IsActive,
    int MaxAgents,
    int MaxTickets,
    DateTime CreatedAt,
    TenantSettingsResponse? Settings
);