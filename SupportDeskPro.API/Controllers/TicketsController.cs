/// <summary>
/// REST controller for ticket management.
/// Customers create and view their own tickets.
/// AI suggestion for category and priority while creating a ticket.
/// Agents view and update tickets assigned to them.
/// AI draft reply generation for agents to review and edit.
/// Admins manage all tickets including assignment.
/// Upload supporting documnents
/// </summary>
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportDeskPro.Application.Features.Tenants.GetTenants;
using SupportDeskPro.Application.Features.Tickets.AIAnalyseSentiment;
using SupportDeskPro.Application.Features.Tickets.AICategorizationSuggest;
using SupportDeskPro.Application.Features.Tickets.AIDraftReply;
using SupportDeskPro.Application.Features.Tickets.AIGetSimilarTickets;
using SupportDeskPro.Application.Features.Tickets.AssignTicket;
using SupportDeskPro.Application.Features.Tickets.CreateTicket;
using SupportDeskPro.Application.Features.Tickets.GetAttachmentDownloadUrl;
using SupportDeskPro.Application.Features.Tickets.GetMyTickets;
using SupportDeskPro.Application.Features.Tickets.GetTicketById;
using SupportDeskPro.Application.Features.Tickets.GetTicketHistory;
using SupportDeskPro.Application.Features.Tickets.GetTickets;
using SupportDeskPro.Application.Features.Tickets.UpdateTicket;
using SupportDeskPro.Application.Features.Tickets.UpdateTicketStatus;
using SupportDeskPro.Application.Features.Tickets.UploadAttachment;
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

    /// <summary>
    /// Retrieves paginated list of all tickets in the current tenant.
    /// Supports filtering by status, priority, category, assigned agent and SLA breach.
    /// Supports keyword search across ticket title, description and customer name.
    /// Results ordered by creation date descending — newest first.
    /// </summary>
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

    /// <summary>
    /// Retrieves paginated list of tickets created by the currently authenticated customer.
    /// Customers can only see their own tickets — other customers tickets are not visible.
    /// Supports filtering by ticket status.
    /// CustomerId extracted from JWT claims automatically.
    /// </summary>
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

    /// <summary>
    /// Retrieves full ticket detail including AI category suggestions,
    /// SLA due dates, assigned agent and customer information.
    /// Accessible by Admin, Agent and the ticket's own Customer.
    /// Returns 404 if ticket does not exist in the current tenant.
    /// </summary>
    // GET /api/tickets/{id} (Admin, Agent, Customer)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetTicketByIdQuery(id));
        return Ok(ApiResponse<TicketDetailResponse>.Ok(result));
    }

    /// <summary>
    /// Creates a new support ticket for the authenticated customer.
    /// Automatically assigns SLA policy based on ticket priority.
    /// Calculates SLA first response and resolution due dates.
    /// Generates sequential tenant-scoped ticket number starting at 1001.
    /// Logs initial Open status to ticket status history.
    /// </summary>
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

    /// <summary>
    /// Updates ticket title, description, category and priority.
    /// Cannot update a closed ticket — returns 400 if attempted.
    /// Validates category is active and belongs to current tenant.
    /// Updates LastActivityAt timestamp on every change.
    /// </summary>
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

    /// <summary>
    /// Changes the status of a ticket — Open, InProgress, Resolved or Closed.
    /// Every status change is logged to TicketStatusHistory as an audit trail.
    /// Sets ResolvedAt timestamp when status changes to Resolved.
    /// Sets ClosedAt timestamp when status changes to Closed.
    /// Cannot change status of an already closed ticket.
    /// </summary>
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

    /// <summary>
    /// Assigns or unassigns a ticket to an agent.
    /// Null AgentId unassigns the ticket.
    /// Auto-transitions ticket status from Open to InProgress on assignment.
    /// Assignment logged to TicketAssignmentHistory as audit trail.
    /// Validates agent is active and belongs to the current tenant.
    /// Cannot assign a closed ticket.
    /// </summary>
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

    /// <summary>
    /// Retrieves the full status change history for a ticket in chronological order.
    /// Shows every status transition including who changed it, when and any notes.
    /// Used for audit trail and SLA reporting on the ticket detail screen.
    /// Returns 404 if ticket does not exist in the current tenant.
    /// </summary>
    // GET /api/tickets/{id}/history (Admin and Agent)
    [HttpGet("{id}/history")]
    [Authorize(Roles = "Admin,Agent,Customer")]
    public async Task<IActionResult> GetHistory(Guid id)
    {
        var result = await _mediator.Send(new GetTicketHistoryQuery(id));

        return Ok(ApiResponse<List<TicketStatusHistoryResponse>>.Ok(result));
    }

    /// <summary>
    /// Returns AI suggested category and priority for a ticket.
    /// Called from frontend while customer is typing.
    /// AI failure returns a default response — never throws.
    /// </summary>
    // POST /api/tickets/ai-suggest
    [HttpPost("ai-suggest")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> AISuggest([FromBody] AISuggestRequest request)
    {
        var result = await _mediator.Send(new AISuggestQuery(request.Title, request.Description));

        return Ok(ApiResponse<AISuggestResponse>.Ok(result));
    }

    /// <summary>
    /// Generates an AI-drafted reply for the agent to review and edit.
    /// Based on ticket content and full conversation history.
    /// Agent always reviews before sending — AI never sends automatically.
    /// </summary>
    [HttpPost("{id}/ai-draft-reply")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> AIDraftReply(Guid id,[FromBody] AIDraftReplyRequest request)
    {
        var result = await _mediator.Send(new AIDraftReplyQuery(id, request.IsInternal));
        return Ok(ApiResponse<AIDraftReplyResponse>.Ok(result));
    }


    /// <summary>
    /// Returns top 3 semantically similar resolved tickets.
    /// Two-stage: SQL pre-filters candidates, Claude scores semantically.
    /// Admin and Agent only — not visible to customers.
    /// Returns empty list if no similar tickets found or AI fails.
    /// </summary>
    [HttpGet("{id}/similar")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> GetSimilarTickets(Guid id)
    {
        var result = await _mediator.Send(new AIGetSimilarTicketsQuery(id));
        return Ok(ApiResponse<List<AISimilarTicketResponse>>.Ok(result));
    }

    /// <summary>
    /// Analyses customer sentiment for a ticket.
    /// Reads description and all customer replies to detect emotional tone.
    /// Returns level (Frustrated/Concerned/Neutral), trigger phrases, and advice.
    /// Admin and Agent only — helps agents adjust their response approach.
    /// Returns Neutral if AI fails — never blocks the agent.
    /// </summary>
    [HttpGet("{id}/sentiment")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> AnalyseSentiment(Guid id)
    {
        var result = await _mediator.Send(new AIAnalyseSentimentQuery(id));
        return Ok(ApiResponse<AISentimentAnalysisResponse>.Ok(result));
    }


    /// <summary>
    /// Uploads a file attachment to a ticket or comment.
    /// File is stored in Azure Blob Storage.
    /// Metadata (name, size, URL) saved to TicketAttachments table.
    /// Max file size: 10MB.
    /// Allowed types: images, PDF, Word, Excel, plain text.
    /// </summary>
    [HttpPost("{id}/attachments")]
    public async Task<IActionResult> UploadAttachment(Guid id, IFormFile file,
        [FromQuery] Guid? commentId,
        [FromServices] ICurrentTenantService tenantService)
    {
        var uploadedById = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        using var stream = file.OpenReadStream();

        var result = await _mediator.Send(
            new UploadAttachmentCommand(
                TicketId: id,
                CommentId: commentId,
                UploadedById: uploadedById,
                TenantId: tenantService.TenantId ?? Guid.Empty,
                FileStream: stream,
                OriginalFileName: file.FileName,
                ContentType: file.ContentType,
                FileSizeBytes: file.Length));

        return Ok(ApiResponse<AttachmentResponse>.Ok(result,"File uploaded successfully."));
    }

    /// <summary>
    /// Generates a time-limited SAS signed URL for secure file download.
    /// Container is private — direct blob URL access is blocked.
    /// Signed URL expires after 24 hours.
    /// </summary>
    [HttpGet("{id}/attachments/{attachmentId}/download")]
    public async Task<IActionResult> DownloadAttachment(Guid id,Guid attachmentId)
    {
        var sasUrl = await _mediator.Send(new GetAttachmentDownloadUrlQuery(id, attachmentId));

        return Ok(ApiResponse<string>.Ok(sasUrl));
    }
}
