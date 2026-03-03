/// <summary>
/// Command model for activating or deactivating a category (Admin only).
/// Deactivated categories are hidden from ticket creation form.
/// </summary>
using MediatR;

namespace SupportDeskPro.Application.Features.Categories.UpdateCategoryStatus;

public record UpdateCategoryStatusCommand(
    Guid CategoryId,
    bool IsActive
) : IRequest<UpdateCategoryStatusResult>;

public record UpdateCategoryStatusResult(bool Success, string Message);