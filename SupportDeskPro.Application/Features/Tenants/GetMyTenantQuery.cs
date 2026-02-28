using MediatR;
using SupportDeskPro.Contracts.Tenants;

namespace SupportDeskPro.Application.Features.Tenants.GetMyTenant;

// Query model for Admin to retrieve their own tenant details
public record GetMyTenantQuery : IRequest<TenantDetailResponse?>;