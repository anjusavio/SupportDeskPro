// Command model for Admin to update their own tenant settings
using MediatR;

namespace SupportDeskPro.Application.Features.Tenants.UpdateTenantSettings;

public record UpdateTenantSettingsCommand(
    Guid TenantId,
    string TimeZone,
    string WorkingHoursStart,
    string WorkingHoursEnd,
    string WorkingDays,
    int AutoCloseAfterDays,
    bool AllowCustomerSelfRegistration
) : IRequest<UpdateTenantSettingsResult>;

public record UpdateTenantSettingsResult(
    bool Success,
    string Message
);