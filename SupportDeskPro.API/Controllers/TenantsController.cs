using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportDeskPro.Application.Features.Tenants.CreateTenant;
using SupportDeskPro.Application.Features.Tenants.GetMyTenant;
using SupportDeskPro.Application.Features.Tenants.GetTenantById;
using SupportDeskPro.Application.Features.Tenants.GetTenants;
using SupportDeskPro.Application.Features.Tenants.UpdateTenant;
using SupportDeskPro.Application.Features.Tenants.UpdateTenantSettings;
using SupportDeskPro.Application.Features.Tenants.UpdateTenantStatus;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Common;
using SupportDeskPro.Contracts.Tenants;

namespace SupportDeskPro.API.Controllers;

// REST controller for tenant management — SuperAdmin manages all, Admin views own tenant
[ApiController]
[Route("api/tenants")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/tenants (SuperAdmin only)
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(
            new GetTenantsQuery(page, pageSize, isActive));

        return Ok(ApiResponse<PagedResult<TenantResponse>>.Ok(
            result));
    }

    // POST /api/tenants (SuperAdmin only)
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest request)
    {
        var command = new CreateTenantCommand(
            request.Name,
            request.Slug,
            request.PlanType,
            request.MaxAgents,
            request.MaxTickets);

        var result = await _mediator.Send(command);

        if (!result.Success)
            return Conflict(
                ApiResponse<string>.Fail(result.Message));

        return CreatedAtAction(
            nameof(GetAll),
            ApiResponse<string>.Ok(
                result.TenantId.ToString()!,
                result.Message));
    }

    // PATCH /api/tenants/{id}/status (SuperAdmin only)
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateStatus(Guid id,[FromBody] UpdateTenantStatusRequest request)
    {
        var result = await _mediator.Send(
            new UpdateTenantStatusCommand(id, request.IsActive));

        if (!result.Success)
            return NotFound(
                ApiResponse<string>.Fail(result.Message));

        return Ok(ApiResponse<string>.Ok(
            result.Message, result.Message));
    }

    // GET /api/tenants/my (Admin only)
    [HttpGet("my")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetMyTenant()
    {
        var result = await _mediator
            .Send(new GetMyTenantQuery());

        if (result == null)
            return NotFound(
                ApiResponse<string>.Fail("Tenant not found."));

        return Ok(ApiResponse<TenantDetailResponse>.Ok(result));
    }

    // GET /api/tenants/{id} (SuperAdmin only)
    [HttpGet("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(
            new GetTenantByIdQuery(id));

        if (result == null)
            return NotFound(
                ApiResponse<string>.Fail("Tenant not found."));

        return Ok(ApiResponse<TenantDetailResponse>.Ok(result));
    }

    // PUT /api/tenants/{id} (SuperAdmin only)
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTenantRequest request)
    {
        var result = await _mediator.Send(
            new UpdateTenantCommand(
                id,
                request.Name,
                request.PlanType,
                request.MaxAgents,
                request.MaxTickets));

        if (!result.Success)
            return NotFound(
                ApiResponse<string>.Fail(result.Message));

        return Ok(ApiResponse<string>.Ok(
            result.Message, result.Message));
    }

    // PUT /api/tenants/my/settings (Admin only)
    [HttpPut("my/settings")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateMySettings(
        [FromBody] UpdateTenantSettingsRequest request,
        [FromServices] ICurrentTenantService tenantService)
    {
        if (tenantService.TenantId == null)
            return BadRequest(
                ApiResponse<string>.Fail("Tenant not found."));

        var result = await _mediator.Send(
            new UpdateTenantSettingsCommand(
                tenantService.TenantId.Value,
                request.TimeZone,
                request.WorkingHoursStart,
                request.WorkingHoursEnd,
                request.WorkingDays,
                request.AutoCloseAfterDays,
                request.AllowCustomerSelfRegistration));

        if (!result.Success)
            return NotFound(
                ApiResponse<string>.Fail(result.Message));

        return Ok(ApiResponse<string>.Ok(
            result.Message, result.Message));
    }
}
