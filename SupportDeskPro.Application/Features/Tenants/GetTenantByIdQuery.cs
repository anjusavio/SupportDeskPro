using MediatR;
using SupportDeskPro.Contracts.Tenants;

namespace SupportDeskPro.Application.Features.Tenants.GetTenantById;

// Query model for retrieving single tenant by Id (SuperAdmin only)
public record GetTenantByIdQuery(Guid TenantId)
    : IRequest<TenantDetailResponse?>;