using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;

namespace SupportDeskPro.Application.Features.Auth.ResetPassword;

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordHandler(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<ResetPasswordResult> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate passwords match
        if (request.NewPassword != request.ConfirmPassword)
            throw new Exception("Passwords do not match.");

        // 2. Hash the incoming token — same way it was stored 
        var tokenHash = ComputeHash(request.Token);

        // 3. Find token in DB
        var resetToken = await _db.PasswordResetTokens
            .IgnoreQueryFilters()
            .Include(t => t.User) // load user to update password 
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash,
                cancellationToken);

        // 4. Token not found
        if (resetToken is null)
            throw new Exception("Invalid or expired reset link.");

        // 5. Token expired (1 hour)
        if (resetToken.ExpiresAt < DateTime.UtcNow)
        {
            // Clean up expired token
            _db.PasswordResetTokens.Remove(resetToken);
            await _db.SaveChangesAsync(cancellationToken);
            throw new Exception("Reset link has expired. Please request a new one.");
        }

        // 6. Update password
        resetToken.User.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        resetToken.User.UpdatedAt = DateTime.UtcNow;

        // 7. Delete token — cannot be reused 
        _db.PasswordResetTokens.Remove(resetToken);

        await _db.SaveChangesAsync(cancellationToken);

        return new ResetPasswordResult("Password reset successfully. You can now login.");
    }

    // Same hash method as RegisterHandler — must match exactly 
    private static string ComputeHash(string input)
    {
        var bytes = System.Security.Cryptography
            .SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}