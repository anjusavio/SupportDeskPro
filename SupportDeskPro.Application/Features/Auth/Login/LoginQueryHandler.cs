using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Auth;
using SupportDeskPro.Domain.Entities;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Auth.Login;

/// <summary>
/// Handles user authentication — validates credentials and issues JWT tokens.
/// Uses IgnoreQueryFilters to bypass tenant isolation during login
/// since no JWT token exists yet to identify the tenant.
/// Throws BusinessValidationException for invalid credentials — intentionally
/// vague to prevent user enumeration attacks.
/// </summary>
public class LoginQueryHandler
    : IRequestHandler<LoginQuery, LoginResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;

    public LoginQueryHandler(
        IApplicationDbContext db,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResult> Handle(
        LoginQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Find user by email — bypass tenant filter (no token yet)
        var user = await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(
                u => u.Email == request.Email.ToLower()
                     && !u.IsDeleted,
                cancellationToken);

        // 2. Intentionally vague — prevents user enumeration attacks
        if (user == null ||
            !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new BusinessValidationException(
                "Invalid email or password.");

        // 3. Check account is active
        if (!user.IsActive)
            throw new BusinessValidationException(
                "Your account has been deactivated. Please contact support.");

        // 4. Generate tokens
        var tenantName = user.Tenant?.Name ?? "Platform";
        var accessToken = _jwtTokenService.GenerateAccessToken(user, tenantName);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // 5. Save hashed refresh token
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = ComputeHash(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _db.RefreshTokens.Add(refreshTokenEntity);

        // 6. Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        // 7. Build response
        var response = new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresIn: 900,
            User: new UserDto(
                Id: user.Id,
                FirstName: user.FirstName,
                LastName: user.LastName,
                Email: user.Email,
                Role: user.Role.ToString(),
                TenantId: user.TenantId,
                TenantName: tenantName));

        return new LoginResult(response);
    }

    private static string ComputeHash(string input)
    {
        var bytes = System.Security.Cryptography
            .SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
