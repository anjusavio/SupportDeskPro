using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Auth.ChangePassword;

public class ChangePasswordHandler
    : IRequestHandler<ChangePasswordCommand, ChangePasswordResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordHandler(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<ChangePasswordResult> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate new passwords match
        if (request.NewPassword != request.ConfirmPassword)
            throw new BusinessValidationException("New passwords do not match.");

        // 2. Cannot reuse same password
        if (request.CurrentPassword == request.NewPassword)
            throw new BusinessValidationException(
                "New password must be different from your current password.");

        // 3. Find user
        var user = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.Id == request.UserId && !u.IsDeleted,
                cancellationToken);

        if (user is null)
            throw new NotFoundException("User", request.UserId);

        // 4. Verify current password
        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new BusinessValidationException(
                "Current password is incorrect.");

        // 5. Update password
        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // 6. Invalidate all refresh tokens — forces re-login on other devices 
        var refreshTokens = await _db.RefreshTokens
            .IgnoreQueryFilters()
            .Where(t => t.UserId == user.Id)
            .ToListAsync(cancellationToken);

        if (refreshTokens.Any())
            _db.RefreshTokens.RemoveRange(refreshTokens);

        await _db.SaveChangesAsync(cancellationToken);

        return new ChangePasswordResult("Password changed successfully.");
    }
}