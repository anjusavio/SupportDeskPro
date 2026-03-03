/// <summary>
/// Query model for retrieving all SLA policies for Admin management view.
/// </summary>
using MediatR;
using SupportDeskPro.Application.Features.Tenants.GetTenants;
using SupportDeskPro.Contracts.SLAPolicies;

namespace SupportDeskPro.Application.Features.SLAPolicies.GetSLAPolicies;

public record GetSLAPoliciesQuery(
    int Page = 1,
    int PageSize = 20,
    bool? IsActive = null
) : IRequest<PagedResult<SLAPolicyResponse>>;