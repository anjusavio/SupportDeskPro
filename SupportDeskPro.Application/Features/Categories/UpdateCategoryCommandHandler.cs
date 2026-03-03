/// <summary>
/// Handles category update — prevents circular parent reference
/// and validates name uniqueness excluding current category.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Categories.UpdateCategory;

public class UpdateCategoryCommandHandler
    : IRequestHandler<UpdateCategoryCommand, UpdateCategoryResult>
{
    private readonly IApplicationDbContext _db;

    public UpdateCategoryCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UpdateCategoryResult> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Find category
        var category = await _db.Categories
            .FirstOrDefaultAsync(
                c => c.Id == request.CategoryId,
                cancellationToken)
            ?? throw new NotFoundException(
                "Category", request.CategoryId);

        // 2. Check name unique — exclude current category
        var nameExists = await _db.Categories
            .AnyAsync(c => c.Name.ToLower() == request.Name.ToLower()
                           && c.Id != request.CategoryId,
                cancellationToken);

        if (nameExists)
            throw new ConflictException(
                $"Category '{request.Name}' already exists.");

        // 3. Prevent category being its own parent
        if (request.ParentCategoryId == request.CategoryId)
            throw new BusinessValidationException(
                "Category cannot be its own parent.");

        // 4. Update
        category.Name = request.Name.Trim();
        category.Description = request.Description?.Trim();
        category.ParentCategoryId = request.ParentCategoryId;
        category.SortOrder = request.SortOrder;

        await _db.SaveChangesAsync(cancellationToken);

        return new UpdateCategoryResult(
            true, "Category updated successfully.");
    }
}