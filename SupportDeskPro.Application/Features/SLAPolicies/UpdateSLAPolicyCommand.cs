/// <summary>
/// Command model for updating SLA policy name and response time targets.
/// </summary>
using MediatR;

namespace SupportDeskPro.Application.Features.SLAPolicies.UpdateSLAPolicy;

public record UpdateSLAPolicyCommand(
    Guid SLAPolicyId,
    string Name,
    int FirstResponseTimeMinutes,
    int ResolutionTimeMinutes
) : IRequest<UpdateSLAPolicyResult>;

public record UpdateSLAPolicyResult(
    bool Success,
    string Message
);