using MediatR;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Entities;
using SupportDeskPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace SupportDeskPro.Application.Features.Auth.Register
{
    public class RegisterCommandHandler
     : IRequestHandler<RegisterCommand, RegisterResult>
    {
        private readonly IApplicationDbContext _db;  
        private readonly IEmailService _emailService;
        private readonly IPasswordHasher _passwordHasher;

        public RegisterCommandHandler(
            IApplicationDbContext db,              
            IEmailService emailService,
            IPasswordHasher passwordHasher)
        {
            _db = db;
            _emailService = emailService;
            _passwordHasher = passwordHasher;
        }
        public async Task<RegisterResult> Handle(
            RegisterCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Validate passwords match
            if (request.Password != request.ConfirmPassword)
                return new RegisterResult(false, "Passwords do not match.");

            // 2. Check if email already exists in this tenant
            var exists = await _db.Users
                .IgnoreQueryFilters() //— bypasses global filter to check all users
                .AnyAsync(u => u.Email == request.Email.ToLower() 
                        && !u.IsDeleted,
                    cancellationToken);

            if (exists)
                return new RegisterResult(false,
                    "An account with this email already exists.");

            // 3. Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 4. Create user
            var user = new User
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = request.Email.ToLower().Trim(),
                PasswordHash = passwordHash,
                Role = UserRole.Customer,
                IsActive = true,
                IsEmailVerified = false
            };

            // 5. Generate email verification token
            var verificationToken = Convert.ToBase64String(
                System.Security.Cryptography.RandomNumberGenerator
                    .GetBytes(32));

            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = ComputeHash(verificationToken),
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            // 6. Save to database
            _db.Users.Add(user);
            _db.PasswordResetTokens.Add(resetToken);
            await _db.SaveChangesAsync(cancellationToken);

            // 7. Send verification email
            await _emailService.SendEmailVerificationAsync(
                user.Email, user.FirstName, verificationToken);

            return new RegisterResult(true,
                "Registration successful! Please check your email.");
        }

        private static string ComputeHash(string input)
        {
            var bytes = System.Security.Cryptography
                .SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
        }
    }
}
