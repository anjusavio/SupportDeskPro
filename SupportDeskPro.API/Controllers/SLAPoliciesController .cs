/// <summary>
/// REST controller for SLA policy management.
/// All endpoints restricted to Admin role.
/// SLA policies define response and resolution time targets per ticket priority.
/// </summary>
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportDeskPro.Application.Features.SLAPolicies.CreateSLAPolicy;
using SupportDeskPro.Application.Features.SLAPolicies.GetSLAPolicies;
using SupportDeskPro.Application.Features.SLAPolicies.GetSLAPolicyById;
using SupportDeskPro.Application.Features.SLAPolicies.UpdateSLAPolicy;
using SupportDeskPro.Application.Features.SLAPolicies.UpdateSLAPolicyStatus;
using SupportDeskPro.Application.Features.Tenants.GetTenants;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Common;
using SupportDeskPro.Contracts.SLAPolicies;

namespace SupportDeskPro.API.Controllers;

[ApiController]
[Route("api/sla-policies")]
[Authorize(Roles = "Admin")]
public class SLAPoliciesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SLAPoliciesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieves paginated list of all SLA policies in the current tenant.
    /// Supports filtering by active status.
    /// Results ordered by priority — Critical first down to Low.
    /// </summary>
    // GET /api/sla-policies
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new GetSLAPoliciesQuery(page, pageSize, isActive));
        return Ok(ApiResponse<PagedResult<SLAPolicyResponse>>.Ok(result));
    }

    /// <summary>
    /// Retrieves single SLA policy detail by Id.
    /// Returns first response time and resolution time targets in minutes.
    /// Returns 404 if policy does not exist in the current tenant.
    /// </summary>
    // GET /api/sla-policies/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetSLAPolicyByIdQuery(id));
        return Ok(ApiResponse<SLAPolicyResponse>.Ok(result));
    }

    /// <summary>
    /// Creates a new SLA policy for a specific ticket priority.
    /// Priority values: 1=Low, 2=Medium, 3=High, 4=Critical.
    /// Enforces one active policy per priority per tenant.
    /// Resolution time must be greater than first response time.
    /// Returns 409 if an active policy for the given priority already exists.
    /// </summary>
    // POST /api/sla-policies
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateSLAPolicyRequest request,
        [FromServices] ICurrentTenantService tenantService)
    {
        var result = await _mediator.Send(
            new CreateSLAPolicyCommand(
                tenantService.TenantId ?? Guid.Empty,
                request.Name,
                request.Priority,
                request.FirstResponseTimeMinutes,
                request.ResolutionTimeMinutes));

        return CreatedAtAction(nameof(GetById),
            new { id = result.SLAPolicyId },
            ApiResponse<string>.Ok(
                result.SLAPolicyId.ToString()!,
                result.Message));
    }

    /// <summary>
    /// Updates SLA policy name, first response time and resolution time targets.
    /// Priority cannot be changed after creation — deactivate and create new instead.
    /// Resolution time must always be greater than first response time.
    /// Returns 404 if policy does not exist in the current tenant.
    /// </summary>
    // PUT /api/sla-policies/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateSLAPolicyRequest request)
    {
        var result = await _mediator.Send(
            new UpdateSLAPolicyCommand(
                id,
                request.Name,
                request.FirstResponseTimeMinutes,
                request.ResolutionTimeMinutes));

        return Ok(ApiResponse<string>.Ok(result.Message));
    }

    /// <summary>
    /// Activates or deactivates an SLA policy.
    /// Deactivated policies are excluded from new ticket SLA assignments.
    /// Existing ticket SLA deadlines are not affected by deactivation.
    /// Returns 404 if policy does not exist in the current tenant.
    /// </summary>
    // PATCH /api/sla-policies/{id}/status
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateSLAPolicyStatusRequest request)
    {
        var result = await _mediator.Send(new UpdateSLAPolicyStatusCommand(id, request.IsActive));
        return Ok(ApiResponse<string>.Ok(result.Message));
    }
}
