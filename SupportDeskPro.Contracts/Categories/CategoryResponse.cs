/// <summary>
/// Response models for category list, detail and dropdown views.
/// </summary>
namespace SupportDeskPro.Contracts.Categories;

public record CategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    string? ParentCategoryName,
    int SortOrder,
    bool IsActive,
    int TicketCount
);

public record CategorySummaryResponse(
    Guid Id,
    string Name,
    Guid? ParentCategoryId,
    string? ParentCategoryName
);