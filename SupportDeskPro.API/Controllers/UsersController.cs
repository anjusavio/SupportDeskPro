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
using SupportDeskPro.Domain.Entities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace SupportDeskPro.API.Controllers;

// REST controller for user management — Admin manages users, all roles manage own profile


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

    // GET /api/users (Admin only)
    [HttpGet]
    [Authorize(Roles = "Admin")]
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

    // POST /api/users/invite-agent (Admin only)
    [HttpPost("invite-agent")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> InviteAgent(
        [FromBody] InviteAgentRequest request,
        [FromServices] ICurrentTenantService tenantService)
    {
        if (tenantService.TenantId == null)
            return BadRequest(ApiResponse<string>.Fail("Tenant not found."));

        var result = await _mediator.Send(
            new InviteAgentCommand(
                tenantService.TenantId.Value,
                request.FirstName,
                request.LastName,
                request.Email));

        if (!result.Success)
            return Conflict( ApiResponse<string>.Fail(result.Message));

        return Ok(ApiResponse<string>.Ok(
            result.UserId.ToString()!,
            result.Message));
    }

    // PATCH /api/users/{id}/status (Admin only)
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id,[FromBody] UpdateUserStatusRequest request)
    {
        var result = await _mediator.Send(
            new UpdateUserStatusCommand(id, request.IsActive));

        if (!result.Success)
            return NotFound(ApiResponse<string>.Fail(result.Message));

        return Ok(ApiResponse<string>.Ok(result.Message, result.Message));
    }

    // PUT /api/users/profile (all authenticated users)
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userIdClaim = User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        if (userIdClaim == null)
            return Unauthorized();

        var result = await _mediator.Send(
            new UpdateProfileCommand(
                Guid.Parse(userIdClaim),
                request.FirstName,
                request.LastName));

        if (!result.Success)
            return NotFound( ApiResponse<string>.Fail(result.Message));

        return Ok(ApiResponse<string>.Ok( result.Message, result.Message));
    }

    // GET /api/users/agents (Admin + Agent)
    [HttpGet("agents")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> GetAgents()
    {
        var result = await _mediator.Send(new GetAgentsQuery());

        return Ok(ApiResponse<List<AgentSummaryResponse>>.Ok(result));
    }

    // GET /api/users/agents/workload (Admin only)
    [HttpGet("agents/workload")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAgentWorkload()
    {
        var result = await _mediator.Send(new GetAgentWorkloadQuery());

        return Ok(ApiResponse<List<AgentWorkloadResponse>>.Ok(result));
    }


    // GET /api/users/{id} (Admin only)
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send( new GetUserByIdQuery(id));

        if (result == null)
            return NotFound(ApiResponse<string>.Fail("User not found."));

        return Ok(ApiResponse<UserResponse>.Ok(result));
    }

    // PATCH /api/users/{id}/role (Admin only)
    [HttpPatch("{id}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateRole(
        Guid id,
        [FromBody] UpdateUserRoleRequest request)
    {
        var result = await _mediator.Send(
            new UpdateUserRoleCommand(id, request.Role));

        if (!result.Success)
            return BadRequest(ApiResponse<string>.Fail(result.Message));

        return Ok(ApiResponse<string>.Ok(result.Message, result.Message));
    }
}
