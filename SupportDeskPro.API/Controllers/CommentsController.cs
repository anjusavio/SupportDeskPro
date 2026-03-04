/// <summary>
/// REST controller for ticket comment management.
/// All authenticated users can read and post comments.
/// Internal notes visible to Admin and Agent only.
/// Only comment author can edit their own comment.
/// Only Admin can delete any comment — others can delete own only.
/// </summary>
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportDeskPro.Application.Features.Comments.CreateComment;
using SupportDeskPro.Application.Features.Comments.DeleteComment;
using SupportDeskPro.Application.Features.Comments.GetComments;
using SupportDeskPro.Application.Features.Comments.UpdateComment;
using SupportDeskPro.Contracts.Comments;
using SupportDeskPro.Contracts.Common;
using System.Security.Claims;

namespace SupportDeskPro.API.Controllers;

[ApiController]
[Route("api/tickets/{ticketId}/comments")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieves all comments for a specific ticket.
    /// Customers see only public comments — internal agent notes are hidden.
    /// Agents and Admins see all comments including internal notes.
    /// Comments returned in chronological order oldest first.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid ticketId)
    {
        var isCustomer = User.IsInRole("Customer");

        var result = await _mediator.Send(new GetCommentsQuery(ticketId, isCustomer));
        return Ok(ApiResponse<List<CommentResponse>>.Ok(result));
    }

    /// <summary>
    /// Posts a new comment on a ticket.
    /// IsInternal = true creates an agent-only note hidden from customers.
    /// Customers are blocked from posting internal notes.
    /// First public agent reply automatically sets SLA FirstResponseAt timestamp.
    /// Cannot post on a closed ticket.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        Guid ticketId,
        [FromBody] CreateCommentRequest request)
    {
        var authorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var authorRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        var result = await _mediator.Send(
            new CreateCommentCommand(
                ticketId,
                authorId,
                authorRole,
                request.Body,
                request.IsInternal));

        return Ok(ApiResponse<string>.Ok(result.CommentId.ToString()!, result.Message));
    }

    /// <summary>
    /// Edits the body of an existing comment.
    /// Only the original author can edit their own comment.
    /// Comment is marked as edited with EditedAt timestamp.
    /// </summary>
    [HttpPut("{commentId}")]
    public async Task<IActionResult> Update(
        Guid ticketId,
        Guid commentId,
        [FromBody] UpdateCommentRequest request)
    {
        var requesterId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _mediator.Send(
            new UpdateCommentCommand(
                commentId,
                requesterId,
                request.Body));

        return Ok(ApiResponse<string>.Ok(result.Message));
    }

    /// <summary>
    /// Soft deletes a comment — record remains in DB but hidden from responses.
    /// Admin can delete any comment regardless of author.
    /// Non-admin users can only delete their own comments.
    /// </summary>
    [HttpDelete("{commentId}")]
    public async Task<IActionResult> Delete(
        Guid ticketId,
        Guid commentId)
    {
        var requesterId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var requesterRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        var result = await _mediator.Send(
            new DeleteCommentCommand(
                commentId,
                requesterId,
                requesterRole));

        return Ok(ApiResponse<string>.Ok(result.Message));
    }
}