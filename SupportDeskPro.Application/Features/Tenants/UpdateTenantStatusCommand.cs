using MediatR;

namespace SupportDeskPro.Application.Features.Tenants.UpdateTenantStatus;

// Command model for activating or deactivating a tenant (SuperAdmin only)
public record UpdateTenantStatusCommand(
    Guid TenantId,
    bool IsActive
) : IRequest<UpdateTenantStatusResult>;

public record UpdateTenantStatusResult(
    bool Success,
    string Message
);