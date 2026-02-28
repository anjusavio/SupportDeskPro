using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;

namespace SupportDeskPro.Application.Features.Tenants.UpdateTenantStatus;

// Handles tenant activation/deactivation — deactivated tenants cannot login
public class UpdateTenantStatusCommandHandler
    : IRequestHandler<UpdateTenantStatusCommand, UpdateTenantStatusResult>
{
    private readonly IApplicationDbContext _db;

    public UpdateTenantStatusCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UpdateTenantStatusResult> Handle(
        UpdateTenantStatusCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                t => t.Id == request.TenantId,
                cancellationToken);

        if (tenant == null)
            return new UpdateTenantStatusResult(false, "Tenant not found.");

        tenant.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);

        var status = request.IsActive ? "activated" : "deactivated";
        return new UpdateTenantStatusResult(
            true, $"Tenant {status} successfully.");
    }
}