// Handles updating tenant working hours, timezone and portal settings
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;

namespace SupportDeskPro.Application.Features.Tenants.UpdateTenantSettings;

public class UpdateTenantSettingsCommandHandler
    : IRequestHandler<UpdateTenantSettingsCommand, UpdateTenantSettingsResult>
{
    private readonly IApplicationDbContext _db;

    public UpdateTenantSettingsCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UpdateTenantSettingsResult> Handle(
        UpdateTenantSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var settings = await _db.TenantSettings
            .FirstOrDefaultAsync(
                s => s.TenantId == request.TenantId,
                cancellationToken);

        if (settings == null)
            return new UpdateTenantSettingsResult(
                false, "Tenant settings not found.");

        settings.TimeZone = request.TimeZone;
        settings.WorkingHoursStart = TimeOnly.Parse(request.WorkingHoursStart);
        settings.WorkingHoursEnd = TimeOnly.Parse(request.WorkingHoursEnd);
        settings.WorkingDays = request.WorkingDays;
        settings.AutoCloseAfterDays = request.AutoCloseAfterDays;
        settings.AllowCustomerSelfRegistration =
            request.AllowCustomerSelfRegistration;

        await _db.SaveChangesAsync(cancellationToken);

        return new UpdateTenantSettingsResult(
            true, "Settings updated successfully.");
    }
}