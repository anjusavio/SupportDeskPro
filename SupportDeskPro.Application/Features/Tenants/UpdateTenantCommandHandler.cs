// Handles updating tenant name, plan type and resource limits
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Enums;

namespace SupportDeskPro.Application.Features.Tenants.UpdateTenant;

public class UpdateTenantCommandHandler
    : IRequestHandler<UpdateTenantCommand, UpdateTenantResult>
{
    private readonly IApplicationDbContext _db;

    public UpdateTenantCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UpdateTenantResult> Handle(
        UpdateTenantCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                t => t.Id == request.TenantId,
                cancellationToken);

        if (tenant == null)
            return new UpdateTenantResult(false, "Tenant not found.");

        tenant.Name = request.Name.Trim();
        tenant.PlanType = (PlanType)request.PlanType;
        tenant.MaxAgents = request.MaxAgents;
        tenant.MaxTickets = request.MaxTickets;

        await _db.SaveChangesAsync(cancellationToken);

        return new UpdateTenantResult(true, "Tenant updated successfully.");
    }
}