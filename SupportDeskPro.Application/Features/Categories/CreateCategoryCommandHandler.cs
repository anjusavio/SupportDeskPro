/// <summary>
/// Handles category creation — validates unique name per tenant
/// and parent category belongs to same tenant.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SupportDeskPro.Application.Common;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Entities;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Categories.CreateCategory;

public class CreateCategoryCommandHandler
    : IRequestHandler<CreateCategoryCommand, CreateCategoryResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public CreateCategoryCommandHandler(IApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<CreateCategoryResult> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Check name is unique in this tenant
        var exists = await _db.Categories
            .AnyAsync(c => c.Name.ToLower() == request.Name.ToLower(),
                cancellationToken);

        if (exists)
            throw new ConflictException(
                $"Category '{request.Name}' already exists.");

        // 2. Validate parent category exists in same tenant
        if (request.ParentCategoryId.HasValue)
        {
            var parentExists = await _db.Categories
                .AnyAsync(c => c.Id == request.ParentCategoryId,
                    cancellationToken);

            if (!parentExists)
                throw new NotFoundException(
                    "ParentCategory", request.ParentCategoryId);
        }

        // 3. Create category
        var category = new Category
        {
            TenantId = request.TenantId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ParentCategoryId = request.ParentCategoryId,
            SortOrder = request.SortOrder,
            IsActive = true
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);


        // After saving to DB — invalidate categories cache
        var cacheKey = CacheKeys.ActiveCategories(request.TenantId);
        _cache.Remove(cacheKey); // next request fetches fresh from DB

        return new CreateCategoryResult(
            true, "Category created successfully.", category.Id);
    }
}