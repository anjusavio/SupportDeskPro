using MediatR;

namespace SupportDeskPro.Application.Features.Tenants.DeleteTenant;

//soft delete tenant by id (SuperAdmin only)
public record DeleteTenantCommand(Guid TenantId) : IRequest<DeleteTenantResult>;

public record DeleteTenantResult(bool Success, string Message);