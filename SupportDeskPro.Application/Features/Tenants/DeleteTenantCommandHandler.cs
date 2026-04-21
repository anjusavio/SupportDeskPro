using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Tenants.DeleteTenant;

/// <summary>
/// Soft deletes a tenant — sets IsDeleted flag, does not remove from DB.
/// SuperAdmin only.
/// Deactivates the tenant first to block all logins immediately.
/// All tenant data remains in the database for audit purposes.
/// Hard delete is intentionally not implemented — data retention policy.
/// </summary>
public class DeleteTenantCommandHandler
    : IRequestHandler<DeleteTenantCommand, DeleteTenantResult>
{
    private readonly IApplicationDbContext _db;

    public DeleteTenantCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DeleteTenantResult> Handle(
        DeleteTenantCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(
                t => t.Id == request.TenantId && !t.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException("Tenant", request.TenantId);

        // Deactivate first — blocks all logins immediately
        tenant.IsActive = false;
        tenant.IsDeleted = true;
        tenant.DeletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new DeleteTenantResult(
            true,
            $"Tenant '{tenant.Name}' deleted successfully.");
    }
}