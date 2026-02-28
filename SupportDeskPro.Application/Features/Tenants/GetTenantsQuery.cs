using MediatR;
using SupportDeskPro.Contracts.Common;
using SupportDeskPro.Contracts.Tenants;

namespace SupportDeskPro.Application.Features.Tenants.GetTenants;

// Query model for retrieving paginated list of all tenants (SuperAdmin only)
public record GetTenantsQuery(
    int Page = 1,
    int PageSize = 20,
    bool? IsActive = null
) : IRequest<PagedResult<TenantResponse>>;

public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);