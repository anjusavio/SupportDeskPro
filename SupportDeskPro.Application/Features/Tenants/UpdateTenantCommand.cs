using MediatR;

namespace SupportDeskPro.Application.Features.Tenants.UpdateTenant;

// Command model for updating tenant name, plan and limits (SuperAdmin only)
public record UpdateTenantCommand(
    Guid TenantId,
    string Name,
    string PlanType,
    int MaxAgents,
    int MaxTickets,
    bool IsActive
) : IRequest<UpdateTenantResult>;

public record UpdateTenantResult(
    bool Success,
    string Message
);