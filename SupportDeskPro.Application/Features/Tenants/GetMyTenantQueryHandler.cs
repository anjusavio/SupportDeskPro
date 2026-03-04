using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Tenants;
using SupportDeskPro.Domain.Entities;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Tenants.GetMyTenant;

// Handles fetching current Admin's tenant with settings — scoped by TenantId from JWT
public class GetMyTenantQueryHandler
    : IRequestHandler<GetMyTenantQuery, TenantDetailResponse?>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenantService;

    public GetMyTenantQueryHandler(
        IApplicationDbContext db,
        ICurrentTenantService tenantService)
    {
        _db = db;
        _tenantService = tenantService;
    }

    public async Task<TenantDetailResponse?> Handle(
        GetMyTenantQuery request,
        CancellationToken cancellationToken)
    {
        // Check TenantId from JWT first -Tenant null means no tenant in JWT token , so auth issue
        if (_tenantService.TenantId == null)
            throw new ForbiddenException(
                "No tenant associated with this account.");

        var tenant = await _db.Tenants
            .Include(t => t.Settings)
            .FirstOrDefaultAsync(
                t => t.Id == _tenantService.TenantId,
                cancellationToken);

        //TenantId exists in JWT but Tenant record not found in DB 
        if (tenant == null)
            throw new NotFoundException("Tenant", _tenantService.TenantId);

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