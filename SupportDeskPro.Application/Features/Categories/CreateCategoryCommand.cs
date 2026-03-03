/// <summary>
/// Command model for creating a new ticket category with optional parent for hierarchy.
/// </summary>
using MediatR;

namespace SupportDeskPro.Application.Features.Categories.CreateCategory;

public record CreateCategoryCommand(
    Guid TenantId,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int SortOrder
) : IRequest<CreateCategoryResult>;

public record CreateCategoryResult(
    bool Success,
    string Message,
    Guid? CategoryId = null
);