using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Auth;
using SupportDeskPro.Domain.Entities;


namespace SupportDeskPro.Application.Features.Auth.Login;

public class LoginQueryHandler : IRequestHandler<LoginQuery, LoginResult>
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

    //stateless JWT authentication with access tokens(15 minute expiry) and
    //refresh tokens(7 day expiry) stored as SHA-256 hashes in the database.
    public async Task<LoginResult> Handle(LoginQuery request,CancellationToken cancellationToken)
    {
        // 1. Find user by email
        var user = await _db.Users
            .IgnoreQueryFilters() //— bypasses global filter for login only
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(
                u => u.Email == request.Email.ToLower()
                 && !u.IsDeleted,
                cancellationToken);

        //for testing
        //var test = BCrypt.Net.BCrypt.Verify("password", user.PasswordHash);
        //Console.WriteLine(test);
        //Console.WriteLine(user.PasswordHash.Length);
        //var newHash = BCrypt.Net.BCrypt.HashPassword("password");
        //Console.WriteLine(newHash);

        //var verify = BCrypt.Net.BCrypt.Verify("password", newHash);
        //Console.WriteLine(verify);

        // 2. Validate user exists and password correct
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return new LoginResult(false, "Invalid email or password.", null);

        // 3. Check account is active
        if (!user.IsActive)
            return new LoginResult(false, "Account is deactivated.", null);

        // 4. Generate tokens
        var tenantName = user.Tenant?.Name ?? "Platform";
        var accessToken = _jwtTokenService.GenerateAccessToken(user, tenantName);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // 5. Save refresh token to DB
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = ComputeHash(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7) //7 day expiry
        };

        _db.RefreshTokens.Add(refreshTokenEntity);

        // 6. Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        // 7. Build response
        var response = new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresIn: 900, // 15 minutes in seconds
            User: new UserDto(
                Id: user.Id,
                FirstName: user.FirstName,
                LastName: user.LastName,
                Email: user.Email,
                Role: user.Role.ToString(),
                TenantId: user.TenantId,
                TenantName: tenantName
            )
        );

        return new LoginResult(true, null, response);
    }

    private static string ComputeHash(string input)
    {
        var bytes = System.Security.Cryptography
            .SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}