/// <summary>
/// Handles fetching paginated category list with ticket count per category.
/// Scoped to current tenant via Global Query Filter.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Features.Tenants.GetTenants;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Categories;

namespace SupportDeskPro.Application.Features.Categories.GetCategories;

public class GetCategoriesQueryHandler
    : IRequestHandler<GetCategoriesQuery, PagedResult<CategoryResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetCategoriesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<CategoryResponse>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Categories
            .Include(c => c.ParentCategory)
            .AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(c => c.IsActive == request.IsActive);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CategoryResponse(
                c.Id,
                c.Name,
                c.Description,
                c.ParentCategoryId,
                c.ParentCategory != null
                    ? c.ParentCategory.Name
                    : null,
                c.SortOrder,
                c.IsActive,
                c.Tickets.Count(t => !t.IsDeleted)))
            .ToListAsync(cancellationToken);

        return new PagedResult<CategoryResponse>(
            items, totalCount, request.Page, request.PageSize);
    }
}