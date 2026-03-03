// Query model for paginated user list filtered by role and status (Admin only)
using MediatR;
using SupportDeskPro.Application.Features.Tenants.GetTenants;
using SupportDeskPro.Contracts.Users;

namespace SupportDeskPro.Application.Features.Users.GetUsers;

public record GetUsersQuery(
    int Page = 1,
    int PageSize = 20,
    int? Role = null,
    bool? IsActive = null,
    string? Search = null
) : IRequest<PagedResult<UserResponse>>;