/// <summary>
/// Request model for updating an existing category name and details (Admin only).
/// </summary>
namespace SupportDeskPro.Contracts.Categories;

public record UpdateCategoryRequest(
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int SortOrder
);