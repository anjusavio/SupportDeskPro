using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Entities;
using SupportDeskPro.Domain.Enums;
using SupportDeskPro.Domain.Exceptions;

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

            // 2. Find tenant by slug ← ADD HERE
            var tenant = await _db.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    t => t.Slug == request.TenantSlug.ToLower(),
                    cancellationToken);
            
            //if (tenant == null)
            //    return new RegisterResult(
            //        false, "Invalid tenant. Please check your registration link.");
            if (tenant == null)
                throw new NotFoundException("Tenant", request.TenantSlug);

            if (!tenant.IsActive)
                return new RegisterResult(
                    false, "This organization account is currently inactive.");

            // 3. Check if email already exists in this tenant
            var exists = await _db.Users
                .IgnoreQueryFilters() //— bypasses global filter to check all users
                .AnyAsync(u => u.Email == request.Email.ToLower() 
                        && !u.IsDeleted,
                    cancellationToken);

            if (exists)
                throw new ConflictException("An account with this email already exists.");

            // 4. Hash password
            var passwordHash = _passwordHasher.Hash(request.Password);

            // 5. Create user
            var user = new User
            {
                TenantId = tenant.Id,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = request.Email.ToLower().Trim(),
                PasswordHash = passwordHash,
                Role = UserRole.Customer,
                IsActive = true,
                IsEmailVerified = false
            };

            // 6. Generate email verification token
            var verificationToken = Convert.ToBase64String(
                System.Security.Cryptography.RandomNumberGenerator
                    .GetBytes(32));

            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = ComputeHash(verificationToken),
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            // 7. Save to database
            _db.Users.Add(user);
            _db.PasswordResetTokens.Add(resetToken);
            await _db.SaveChangesAsync(cancellationToken);

            // 8. Send verification email
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
