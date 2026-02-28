namespace SupportDeskPro.Contracts.Tenants;

// Request model for toggling tenant active/inactive status
public record UpdateTenantStatusRequest(
    bool IsActive
);