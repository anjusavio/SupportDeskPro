/// <summary>
/// Handles category activation/deactivation — deactivated categories
/// hidden from ticket creation but existing tickets retain their category.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Categories.UpdateCategoryStatus;

public class UpdateCategoryStatusCommandHandler
    : IRequestHandler<UpdateCategoryStatusCommand, UpdateCategoryStatusResult>
{
    private readonly IApplicationDbContext _db;

    public UpdateCategoryStatusCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UpdateCategoryStatusResult> Handle(
        UpdateCategoryStatusCommand request,
        CancellationToken cancellationToken)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(
                c => c.Id == request.CategoryId,
                cancellationToken)
            ?? throw new NotFoundException(
                "Category", request.CategoryId);

        category.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);

        var status = request.IsActive ? "activated" : "deactivated";
        return new UpdateCategoryStatusResult(true, $"Category {status} successfully.");
    }
}