// Handles role change — only Agent and Customer roles allowed (Admin cannot self-promote)
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Enums;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Users.UpdateUserRole;

public class UpdateUserRoleCommandHandler : IRequestHandler<UpdateUserRoleCommand, UpdateUserRoleResult>
{
    private readonly IApplicationDbContext _db;

    public UpdateUserRoleCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UpdateUserRoleResult> Handle(
        UpdateUserRoleCommand request,
        CancellationToken cancellationToken)
    {
        // Only Agent(3) and Customer(4) roles allowed
        if (request.Role != 3 && request.Role != 4)
            throw new BusinessValidationException(
                "Role must be Agent(3) or Customer(4) only.");

        var user = await _db.Users
            .FirstOrDefaultAsync(
                u => u.Id == request.UserId,
                cancellationToken);

        if (user == null)
            throw new NotFoundException("User", request.UserId);

        user.Role = (UserRole)request.Role;
        await _db.SaveChangesAsync(cancellationToken);

        return new UpdateUserRoleResult( true, $"User role changed to {user.Role} successfully.");
    }
}