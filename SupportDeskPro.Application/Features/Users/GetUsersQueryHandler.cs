// Handles fetching paginated user list scoped to current tenant
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Features.Tenants.GetTenants;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Users;

namespace SupportDeskPro.Application.Features.Users.GetUsers;

public class GetUsersQueryHandler
    : IRequestHandler<GetUsersQuery, PagedResult<UserResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetUsersQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<UserResponse>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        // with AsQueryable() - Can build query step by step based on conditions
        var query = _db.Users.AsQueryable();

        // Filter by role if provided
        if (request.Role.HasValue)
            query = query.Where(u =>
                (int)u.Role == request.Role.Value);

        // Filter by active status if provided
        if (request.IsActive.HasValue)
            query = query.Where(u =>
                u.IsActive == request.IsActive.Value);

        // Search by name or email
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(search) ||
                u.LastName.ToLower().Contains(search) ||
                u.Email.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        Console.WriteLine(query.ToQueryString());


        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserResponse(
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.Role.ToString(),
                u.IsActive,
                u.IsEmailVerified,
                u.LastLoginAt,
                u.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<UserResponse>(
            items, totalCount, request.Page, request.PageSize);
    }
}