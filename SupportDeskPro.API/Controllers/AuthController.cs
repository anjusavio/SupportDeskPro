using MediatR;
using Microsoft.AspNetCore.Mvc;
using SupportDeskPro.Application.Features.Auth.Login;
using SupportDeskPro.Application.Features.Auth.Register;
using SupportDeskPro.Contracts.Auth;
using SupportDeskPro.Contracts.Common;

namespace SupportDeskPro.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator; // ONE dependency only

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password,
            request.ConfirmPassword);

        // Controller just SENDS a message, Doesn't know HOW login works - Doesn't care which class handles it
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(ApiResponse<string>.Fail(result.Message));

        return Ok(ApiResponse<string>.Ok("Registration successful",result.Message));
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var query = new LoginQuery(request.Email, request.Password);
        var result = await _mediator.Send(query);

        if (!result.Success)
            return Unauthorized(ApiResponse<string>.Fail(result.Message!));

        return Ok(ApiResponse<LoginResponse>.Ok(result.Response!));
    }

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