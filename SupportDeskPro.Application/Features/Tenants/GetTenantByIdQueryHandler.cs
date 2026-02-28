
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Tenants;

namespace SupportDeskPro.Application.Features.Tenants.GetTenantById;

// Handles fetching single tenant with settings by Id
public class GetTenantByIdQueryHandler
    : IRequestHandler<GetTenantByIdQuery, TenantDetailResponse?>
{
    private readonly IApplicationDbContext _db;

    public GetTenantByIdQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<TenantDetailResponse?> Handle(
        GetTenantByIdQuery request,
        CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .IgnoreQueryFilters()
            .Include(t => t.Settings)
            .FirstOrDefaultAsync(
                t => t.Id == request.TenantId,
                cancellationToken);

        if (tenant == null) return null;

        return new TenantDetailResponse(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.PlanType.ToString(),
            tenant.IsActive,
            tenant.MaxAgents,
            tenant.MaxTickets,
            tenant.CreatedAt,
            tenant.Settings == null ? null : new TenantSettingsResponse(
                tenant.Settings.TenantId,
                tenant.Settings.TimeZone,
                tenant.Settings.WorkingHoursStart.ToString("HH:mm"),
                tenant.Settings.WorkingHoursEnd.ToString("HH:mm"),
                tenant.Settings.WorkingDays,
                tenant.Settings.AutoCloseAfterDays,
                tenant.Settings.AllowCustomerSelfRegistration)
        );
    }
}