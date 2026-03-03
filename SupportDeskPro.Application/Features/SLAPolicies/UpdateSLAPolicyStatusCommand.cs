/// <summary>
/// Command model for activating or deactivating an SLA policy.
/// Deactivated policies are excluded from ticket SLA assignment.
/// </summary>
using MediatR;

namespace SupportDeskPro.Application.Features.SLAPolicies.UpdateSLAPolicyStatus;

public record UpdateSLAPolicyStatusCommand(Guid SLAPolicyId,bool IsActive) : IRequest<UpdateSLAPolicyStatusResult>;

public record UpdateSLAPolicyStatusResult(bool Success,string Message);