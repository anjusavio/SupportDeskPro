using MediatR;
using SupportDeskPro.Contracts.Tenants;

namespace SupportDeskPro.Application.Features.Tenants.CreateTenant;

// Command and result model for creating a new tenant (SuperAdmin only)
public record CreateTenantCommand(
    string Name,
    string Slug,
    int PlanType,
    int MaxAgents,
    int MaxTickets
) : IRequest<CreateTenantResult>;

public record CreateTenantResult(
    bool Success,
    string Message,
    Guid? TenantId = null
);