namespace SupportDeskPro.Contracts.Tenants;

// Request model for Admin to update their tenant working hours and settings
public record UpdateTenantSettingsRequest(
    string TimeZone,
    string WorkingHoursStart,   // "09:00"
    string WorkingHoursEnd,     // "18:00"
    string WorkingDays,         // "1,2,3,4,5"
    int AutoCloseAfterDays,
    bool AllowCustomerSelfRegistration
);