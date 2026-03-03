/// <summary>
/// Request model for creating a new ticket category (Admin only).
/// </summary>
namespace SupportDeskPro.Contracts.Categories;

public record CreateCategoryRequest(
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int SortOrder
);