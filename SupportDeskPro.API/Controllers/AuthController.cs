using MediatR;
using Microsoft.AspNetCore.Mvc;
using SupportDeskPro.Application.Features.Auth.Login;
using SupportDeskPro.Application.Features.Auth.Register;
using SupportDeskPro.Contracts.Auth;
using SupportDeskPro.Contracts.Common;

namespace SupportDeskPro.API.Controllers;

/// <summary>
/// REST controller for authentication — register, login and current user info.
/// No authorization required for register and login endpoints.
/// JWT token required for GET /me endpoint.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator; // ONE dependency only

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Registers a new customer account for the specified tenant.
    /// TenantSlug identifies which organization the customer belongs to.
    /// Validates tenant exists and is active before creating the account.
    /// Sends email verification link after successful registration.
    /// Returns 409 if email already exists in the tenant.
    /// </summary>
    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password,
            request.ConfirmPassword,
            request.TenantSlug);//— which company they belong to (from tenant table)

        // Controller just SENDS a message, Doesn't know HOW login works - Doesn't care which class handles it
        var result = await _mediator.Send(command);

        //return Ok(ApiResponse<string>.Ok("Registration successful",result.Message));
        return Ok(result.Message);
    }


    /// <summary>
    /// Authenticates a user and returns JWT access token and refresh token.
    /// Access token expires in 15 minutes — use refresh token to get a new one.
    /// Returns 400 for invalid credentials — intentionally vague to prevent
    /// user enumeration attacks.
    /// Returns 400 if account is deactivated.
    /// All exceptions handled by global ExceptionMiddleware.
    /// </summary>
    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var query = new LoginQuery(request.Email, request.Password);
        var result = await _mediator.Send(query);

        return Ok(ApiResponse<LoginResponse>.Ok(result.Response!));
    }

    /// <summary>
    /// Returns the currently authenticated user's profile from JWT claims.
    /// Does not hit the database — reads directly from token claims.
    /// Useful for frontend to display logged-in user info on page load.
    /// Returns UserId, Email, Role and TenantName from token.
    /// Requires valid JWT token in Authorization header.
    /// </summary>
    // GET /api/auth/me
    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(
            System.Security.Claims.ClaimTypes.Email)?.Value;
        var role = User.FindFirst(
            System.Security.Claims.ClaimTypes.Role)?.Value;
        var tenantName = User.FindFirst("TenantName")?.Value;

        return Ok(ApiResponse<object>.Ok(new
        {
            UserId = userId,
            Email = email,
            Role = role,
            TenantName = tenantName
        }));
    }
}