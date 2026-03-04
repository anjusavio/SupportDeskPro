/// <summary>
/// REST controller for ticket management.
/// Customers create and view their own tickets.
/// Agents view and update tickets assigned to them.
/// Admins manage all tickets including assignment.
/// </summary>
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportDeskPro.Application.Features.Tenants.GetTenants;
using SupportDeskPro.Application.Features.Tickets.AssignTicket;
using SupportDeskPro.Application.Features.Tickets.CreateTicket;
using SupportDeskPro.Application.Features.Tickets.GetMyTickets;
using SupportDeskPro.Application.Features.Tickets.GetTicketById;
using SupportDeskPro.Application.Features.Tickets.GetTicketHistory;
using SupportDeskPro.Application.Features.Tickets.GetTickets;
using SupportDeskPro.Application.Features.Tickets.UpdateTicket;
using SupportDeskPro.Application.Features.Tickets.UpdateTicketStatus;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Common;
using SupportDeskPro.Contracts.Tickets;
using System.Security.Claims;

namespace SupportDeskPro.API.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TicketsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/tickets (Admin and Agent)
    [HttpGet]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null,
        [FromQuery] int? priority = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? assignedAgentId = null,
        [FromQuery] bool? isSLABreached = null,
        [FromQuery] string? search = null)
    {
        var result = await _mediator.Send(
            new GetTicketsQuery(
                page, pageSize, status, priority,
                categoryId, assignedAgentId,
                isSLABreached, search));

        return Ok(ApiResponse<PagedResult<TicketResponse>>.Ok(result));
    }

    // GET /api/tickets/my (Customer only)
    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMyTickets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null)
    {
        var customerId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _mediator.Send(new GetMyTicketsQuery(customerId, page, pageSize, status));
        return Ok(ApiResponse<PagedResult<TicketResponse>>.Ok(result));
    }

    // GET /api/tickets/{id} (Admin, Agent, Customer)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetTicketByIdQuery(id));
        return Ok(ApiResponse<TicketDetailResponse>.Ok(result));
    }

    // POST /api/tickets (Customer only)
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Create(
        [FromBody] CreateTicketRequest request,
        [FromServices] ICurrentTenantService tenantService)
    {
        var customerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _mediator.Send(
            new CreateTicketCommand(
                tenantService.TenantId ?? Guid.Empty,
                customerId,
                request.Title,
                request.Description,
                request.CategoryId,
                request.Priority));

        return CreatedAtAction(nameof(GetById),
            new { id = result.TicketId },
            ApiResponse<string>.Ok(
                result.TicketId.ToString()!,
                $"Ticket #{result.TicketNumber} created successfully."));
    }

    // PUT /api/tickets/{id} (Admin and Agent)
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTicketRequest request)
    {
        var result = await _mediator.Send(
            new UpdateTicketCommand(
                id,
                request.Title,
                request.Description,
                request.CategoryId,
                request.Priority));

        return Ok(ApiResponse<string>.Ok(result.Message));
    }

    // PATCH /api/tickets/{id}/status (Admin and Agent)
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateTicketStatusRequest request)
    {
        var changedById = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _mediator.Send(
            new UpdateTicketStatusCommand(
                id, changedById,
                request.Status,
                request.Note));

        return Ok(ApiResponse<string>.Ok(result.Message));
    }

    // PATCH /api/tickets/{id}/assign (Admin only)
    [HttpPatch("{id}/assign")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Assign(
        Guid id,
        [FromBody] AssignTicketRequest request)
    {
        var assignedById = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _mediator.Send(
            new AssignTicketCommand(
                id, assignedById, request.AgentId));

        return Ok(ApiResponse<string>.Ok(result.Message));
    }

    // GET /api/tickets/{id}/history (Admin and Agent)
    [HttpGet("{id}/history")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> GetHistory(Guid id)
    {
        var result = await _mediator.Send(new GetTicketHistoryQuery(id));

        return Ok(ApiResponse<List<TicketStatusHistoryResponse>>.Ok(result));
    }
}
