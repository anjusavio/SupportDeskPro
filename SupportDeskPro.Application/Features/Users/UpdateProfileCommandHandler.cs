// Handles profile update — scoped to current user only
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Users.UpdateProfile;

public class UpdateProfileCommandHandler
    : IRequestHandler<UpdateProfileCommand, UpdateProfileResult>
{
    private readonly IApplicationDbContext _db;

    public UpdateProfileCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UpdateProfileResult> Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        // Validate userId claim exists
    if (string.IsNullOrEmpty(request.UserId))
            throw new ForbiddenException("User identity could not be determined.");

        // Parse userId
        if (!Guid.TryParse(request.UserId, out var userId))
            throw new ForbiddenException( "Invalid user identity.");

        var user = await _db.Users
           .IgnoreQueryFilters()
           .FirstOrDefaultAsync(
               u => u.Id == userId,
               cancellationToken)
           ?? throw new NotFoundException("User", userId);

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        await _db.SaveChangesAsync(cancellationToken);

        return new UpdateProfileResult(true, "Profile updated successfully.");
    }
}