/// <summary>
/// Command model for creating SLA policy — one policy per priority per tenant.
/// </summary>
using MediatR;

namespace SupportDeskPro.Application.Features.SLAPolicies.CreateSLAPolicy;

public record CreateSLAPolicyCommand(
    Guid TenantId,
    string Name,
    int Priority,
    int FirstResponseTimeMinutes,
    int ResolutionTimeMinutes
) : IRequest<CreateSLAPolicyResult>;

public record CreateSLAPolicyResult(
    bool Success,
    string Message,
    Guid? SLAPolicyId = null
);