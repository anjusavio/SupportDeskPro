using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Entities;

namespace SupportDeskPro.Application.Features.Auth.ForgotPassword;

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailService _emailService;

    public ForgotPasswordHandler(
        IApplicationDbContext db,
        IEmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }

    public async Task<ForgotPasswordResult> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        /**
         * SECURITY: Always return success even if email not found.
         * Prevents user enumeration attacks — attacker cannot tell
         * if an email exists in the system by checking the response 
         */
        const string safeMessage =
            "If an account exists with this email, a reset link has been sent.";

        // 1. Find user by email — ignore tenant filter
        var user = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.Email == request.Email.ToLower().Trim()
                     && !u.IsDeleted,
                cancellationToken);

        // Return success even if not found — no info leak 
        if (user is null)
            return new ForgotPasswordResult(safeMessage);

        // 2. Check user is active
        if (!user.IsActive)
            return new ForgotPasswordResult(safeMessage);

        // 3. Delete any existing reset tokens for this user
        //    Prevents multiple valid tokens existing at same time 
        var existingTokens = await _db.PasswordResetTokens
            .IgnoreQueryFilters()
            .Where(t => t.UserId == user.Id)
            .ToListAsync(cancellationToken);

        if (existingTokens.Any())
            _db.PasswordResetTokens.RemoveRange(existingTokens);

        // 4. Generate new reset token — same pattern as RegisterHandler 
        var resetToken = Convert.ToBase64String(
          System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
          .Replace("+", "-")
          .Replace("/", "_")
          .Replace("=", "");

        var passwordResetToken = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = ComputeHash(resetToken),  // store hash not raw token 
            ExpiresAt = DateTime.UtcNow.AddHours(1), // 1 hour expiry for reset
        };

        // 5. Save token
        _db.PasswordResetTokens.Add(passwordResetToken);
        await _db.SaveChangesAsync(cancellationToken);

        // 6. Send reset email
        await _emailService.SendPasswordResetAsync(
            user.Email,
            user.FirstName,
            resetToken); // send raw token — email contains the link 

        return new ForgotPasswordResult(safeMessage);
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