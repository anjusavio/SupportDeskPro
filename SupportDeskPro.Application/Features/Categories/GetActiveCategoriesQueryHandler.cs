/// <summary>
/// Handles fetching active categories for dropdown — ordered by SortOrder.
/// Scoped to current tenant via Global Query Filter.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Categories;
using Microsoft.Extensions.Caching.Memory;
using SupportDeskPro.Application.Common;

namespace SupportDeskPro.Application.Features.Categories.GetActiveCategories;

public class GetActiveCategoriesQueryHandler
    : IRequestHandler<GetActiveCategoriesQuery, List<CategorySummaryResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ICurrentTenantService _currentTenant;

    public GetActiveCategoriesQueryHandler(
        IApplicationDbContext db,
        IMemoryCache cache,
        ICurrentTenantService currentTenant)
    {
        _db = db;
        _cache = cache;
        _currentTenant = currentTenant;
    }

    public async Task<List<CategorySummaryResponse>> Handle(
        GetActiveCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var cacheKey = CacheKeys.ActiveCategories(tenantId);

        // Return from cache if available
        if (_cache.TryGetValue(cacheKey, out List<CategorySummaryResponse>? cachedCategories))
        {
            return cachedCategories!; //  no DB query 
        }

        // Cache miss — query DB
        var categories = await _db.Categories
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

        // Store in cache for 1 hour
        _cache.Set(cacheKey, categories, TimeSpan.FromHours(1));

        return categories;
    }
}