/// <summary>
/// Handles fetching active categories for dropdown — ordered by SortOrder.
/// Scoped to current tenant via Global Query Filter.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Categories;

namespace SupportDeskPro.Application.Features.Categories.GetActiveCategories;

public class GetActiveCategoriesQueryHandler
    : IRequestHandler<GetActiveCategoriesQuery, List<CategorySummaryResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetActiveCategoriesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<CategorySummaryResponse>> Handle(
        GetActiveCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        return await _db.Categories
            .Include(c => c.ParentCategory)
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategorySummaryResponse(
                c.Id,
                c.Name,
                c.ParentCategoryId,
                c.ParentCategory != null
                    ? c.ParentCategory.Name
                    : null))
            .ToListAsync(cancellationToken);
    }
}