using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;

namespace SupportDeskPro.Application.Features.Auth.VerifyEmail;

public class VerifyEmailHandler : IRequestHandler<VerifyEmailCommand, VerifyEmailResult>
{
    private readonly IApplicationDbContext _db;

    public VerifyEmailHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<VerifyEmailResult> Handle(
        VerifyEmailCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Hash the incoming token — same way RegisterHandler stored it
        var tokenHash = ComputeHash(request.Token);

        // 2. Find the token in PasswordResetTokens table
        var resetToken = await _db.PasswordResetTokens
            .IgnoreQueryFilters()
            .Include(t => t.User) // load user to update IsEmailVerified
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash,
                cancellationToken);

        // 3. Token not found
        if (resetToken is null)
            throw new Exception("Invalid verification link.");

        // 4. Token expired (24 hours)
        if (resetToken.ExpiresAt < DateTime.UtcNow)
            throw new Exception("Verification link has expired. Please register again.");

        // 5. Already verified
        if (resetToken.User.IsEmailVerified)
            return new VerifyEmailResult("Email already verified. Please login.");

        // 6. Mark user as verified
        resetToken.User.IsEmailVerified = true;
        resetToken.User.UpdatedAt = DateTime.UtcNow;

        // 7. Delete the token — cannot be reused 
        _db.PasswordResetTokens.Remove(resetToken);

        await _db.SaveChangesAsync(cancellationToken);

        return new VerifyEmailResult("Email verified successfully. You can now login.");
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