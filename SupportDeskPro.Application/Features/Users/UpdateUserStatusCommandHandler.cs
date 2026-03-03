// Handles user activation/deactivation — deactivated users cannot login
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Users.UpdateUserStatus;

public class UpdateUserStatusCommandHandler
    : IRequestHandler<UpdateUserStatusCommand, UpdateUserStatusResult>
{
    private readonly IApplicationDbContext _db;

    public UpdateUserStatusCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UpdateUserStatusResult> Handle(
        UpdateUserStatusCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(
                u => u.Id == request.UserId,
                cancellationToken);

        if (user == null)
            throw new NotFoundException("User", request.UserId); //404 Not Found

        user.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);

        var status = request.IsActive ? "activated" : "deactivated";
        return new UpdateUserStatusResult(true, $"User {status} successfully.");
    }
}