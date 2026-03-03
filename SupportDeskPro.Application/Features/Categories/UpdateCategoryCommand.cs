/// <summary>
/// Command model for updating category name, description and hierarchy (Admin only).
/// </summary>
using MediatR;

namespace SupportDeskPro.Application.Features.Categories.UpdateCategory;

public record UpdateCategoryCommand(
    Guid CategoryId,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int SortOrder
) : IRequest<UpdateCategoryResult>;

public record UpdateCategoryResult(
    bool Success,
    string Message
);