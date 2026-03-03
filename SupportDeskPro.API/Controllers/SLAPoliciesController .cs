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

    // GET /api/sla-policies/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetSLAPolicyByIdQuery(id));
        return Ok(ApiResponse<SLAPolicyResponse>.Ok(result));
    }

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
