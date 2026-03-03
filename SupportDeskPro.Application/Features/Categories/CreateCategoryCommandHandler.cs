/// <summary>
/// Handles category creation — validates unique name per tenant
/// and parent category belongs to same tenant.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Entities;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Categories.CreateCategory;

public class CreateCategoryCommandHandler
    : IRequestHandler<CreateCategoryCommand, CreateCategoryResult>
{
    private readonly IApplicationDbContext _db;

    public CreateCategoryCommandHandler(IApplicationDbContext db)
    {
        _db = db;
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

        return new CreateCategoryResult(
            true, "Category created successfully.", category.Id);
    }
}