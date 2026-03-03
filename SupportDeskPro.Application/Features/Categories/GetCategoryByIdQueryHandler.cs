/// <summary>
/// Handles fetching single category detail — throws NotFoundException if not found.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Categories;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Categories.GetCategoryById;

public class GetCategoryByIdQueryHandler
    : IRequestHandler<GetCategoryByIdQuery, CategoryResponse>
{
    private readonly IApplicationDbContext _db;

    public GetCategoryByIdQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<CategoryResponse> Handle(
        GetCategoryByIdQuery request,
        CancellationToken cancellationToken)
    {
        var category = await _db.Categories
            .Include(c => c.ParentCategory)
            .FirstOrDefaultAsync(
                c => c.Id == request.CategoryId,
                cancellationToken)
            ?? throw new NotFoundException(
                "Category", request.CategoryId);

        return new CategoryResponse(
            category.Id,
            category.Name,
            category.Description,
            category.ParentCategoryId,
            category.ParentCategory?.Name,
            category.SortOrder,
            category.IsActive,
            category.Tickets.Count(t => !t.IsDeleted));
    }
}