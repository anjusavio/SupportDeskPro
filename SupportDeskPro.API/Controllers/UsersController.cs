using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportDeskPro.Application.Features.Tenants.GetTenants;
using SupportDeskPro.Application.Features.Users.GetAgents;
using SupportDeskPro.Application.Features.Users.GetAgentWorkload;
using SupportDeskPro.Application.Features.Users.GetUserById;
using SupportDeskPro.Application.Features.Users.GetUsers;
using SupportDeskPro.Application.Features.Users.InviteAgent;
using SupportDeskPro.Application.Features.Users.UpdateProfile;
using SupportDeskPro.Application.Features.Users.UpdateUserRole;
using SupportDeskPro.Application.Features.Users.UpdateUserStatus;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Common;
using SupportDeskPro.Contracts.Users;
using System.Security.Claims;


namespace SupportDeskPro.API.Controllers;

/// <summary>
/// REST controller for user management.
/// Admin manages all users within their tenant.
/// All authenticated roles can manage their own profile.
/// SuperAdmin can view users across all tenants.
/// </summary>

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieves paginated list of all users in the current tenant.
    /// Supports filtering by role, active status and name/email search.
    /// Admin sees only users within their own tenant — enforced by Global Query Filter.
    /// SuperAdmin can see users across all tenants.
    /// </summary>
    // GET /api/users (Admin only)
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? role = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null) // Search by name or email
    {
        //Admin cannot see users of other tenants -Global Query Filter (tenantid) automatically adds
        var result = await _mediator.Send(new GetUsersQuery(page, pageSize, role, isActive, search));

        return Ok(ApiResponse<PagedResult<UserResponse>>.Ok(result));
    }

    /// <summary>
    /// Invites a new agent to the current tenant by email.
    /// Creates user account with Agent role and temporary password.
    /// Sends welcome email with temporary password via email service.
    /// Returns 409 if email already exists in the tenant.
    /// </summary>
    // POST /api/users/invite-agent (Admin only)
    [HttpPost("invite-agent")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> InviteAgent(
        [FromBody] InviteAgentRequest request,
        [FromServices] ICurrentTenantService tenantService)
    {

        var result = await _mediator.Send(
            new InviteAgentCommand(
                tenantService.TenantId,
                request.FirstName,
                request.LastName,
                request.Email));

        return Ok(ApiResponse<string>.Ok(result.UserId.ToString()!,result.Message));
    }

    /// <summary>
    /// Activates or deactivates a user account within the current tenant.
    /// Deactivated users cannot login — JWT validation will fail.
    /// Returns 404 if user does not exist in the tenant.
    /// </summary>
    // PATCH /api/users/{id}/status (Admin only)
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id,[FromBody] UpdateUserStatusRequest request)
    {
        var result = await _mediator.Send( new UpdateUserStatusCommand(id, request.IsActive));
        return Ok(ApiResponse<string>.Ok(result.Message, result.Message));
    }

    /// <summary>
    /// Updates the first name and last name of the currently authenticated user.
    /// Available to all authenticated roles — each user manages their own profile.
    /// UserId extracted from JWT claims — users cannot update other profiles.
    /// </summary>
    // PUT /api/users/profile (all authenticated users)
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var result = await _mediator.Send(
        new UpdateProfileCommand(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            request.FirstName,
            request.LastName));

        return Ok(ApiResponse<string>.Ok( result.Message, result.Message));
    }

    /// <summary>
    /// Retrieves list of all active agents in the current tenant.
    /// Used to populate agent assignment dropdown on ticket detail screen.
    /// Returns Id, name and email for each active agent.
    /// </summary>
    // GET /api/users/agents (Admin + Agent)
    [HttpGet("agents")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> GetAgents()
    {
        var result = await _mediator.Send(new GetAgentsQuery());
        return Ok(ApiResponse<List<AgentSummaryResponse>>.Ok(result));
    }

    /// <summary>
    /// Retrieves ticket workload summary for all active agents in the tenant.
    /// Shows open, in-progress and resolved today counts per agent.
    /// Used on Admin dashboard to monitor agent capacity and distribution.
    /// Results ordered by total active tickets descending.
    /// </summary>
    // GET /api/users/agents/workload (Admin only)
    [HttpGet("agents/workload")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAgentWorkload()
    {
        var result = await _mediator.Send(new GetAgentWorkloadQuery());
        return Ok(ApiResponse<List<AgentWorkloadResponse>>.Ok(result));
    }

    /// <summary>
    /// Retrieves single user detail by Id within the current tenant.
    /// Returns 404 if user does not exist or belongs to different tenant.
    /// </summary>
    // GET /api/users/{id} (Admin only)
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send( new GetUserByIdQuery(id));
        return Ok(ApiResponse<UserResponse>.Ok(result));
    }

    /// <summary>
    /// Changes the role of a user within the current tenant.
    /// Only Agent(3) and Customer(4) roles are allowed.
    /// Admin cannot promote users to Admin or SuperAdmin roles.
    /// Returns 404 if user does not exist in the tenant.
    /// </summary>
    // PATCH /api/users/{id}/role (Admin only)
    [HttpPatch("{id}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateRole(
        Guid id,
        [FromBody] UpdateUserRoleRequest request)
    {
        var result = await _mediator.Send(new UpdateUserRoleCommand(id, request.Role));
        return Ok(ApiResponse<string>.Ok(result.Message, result.Message));
    }
}
