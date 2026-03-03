/// <summary>
/// Request model for activating or deactivating a category (Admin only).
/// </summary>
namespace SupportDeskPro.Contracts.Categories;

public record UpdateCategoryStatusRequest(
    bool IsActive
);