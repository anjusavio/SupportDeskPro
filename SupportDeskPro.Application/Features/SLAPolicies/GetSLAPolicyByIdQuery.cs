/// <summary>
/// Query model for retrieving single SLA policy detail by Id.
/// </summary>
using MediatR;
using SupportDeskPro.Contracts.SLAPolicies;

namespace SupportDeskPro.Application.Features.SLAPolicies.GetSLAPolicyById;

public record GetSLAPolicyByIdQuery(Guid SLAPolicyId) : IRequest<SLAPolicyResponse>;