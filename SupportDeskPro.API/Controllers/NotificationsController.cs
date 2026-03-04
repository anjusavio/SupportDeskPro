/// <summary>
/// REST controller for user notification management.
/// All authenticated users can view and manage their own notifications.
/// Users can only access their own notifications — enforced in handlers.
/// </summary>
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportDeskPro.Application.Features.Notifications.GetNotifications;
using SupportDeskPro.Application.Features.Notifications.GetUnreadCount;
using SupportDeskPro.Application.Features.Notifications.MarkAllAsRead;
using SupportDeskPro.Application.Features.Notifications.MarkAsRead;
using SupportDeskPro.Application.Features.Tenants.GetTenants;
using SupportDeskPro.Contracts.Common;
using SupportDeskPro.Contracts.Notifications;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading;

namespace SupportDeskPro.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieves paginated list of notifications for the current user.
    /// Supports filtering by read/unread status.
    /// Results ordered newest first.
    /// Each notification includes ticket number for direct navigation.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isRead = null)
    {
        var recipientId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _mediator.Send(
            new GetNotificationsQuery(recipientId, page, pageSize, isRead));

        return Ok(ApiResponse<PagedResult<NotificationResponse>>.Ok(result));
    }

    /// <summary>
    /// Returns the total count of unread notifications for the current user.
    /// Used to display badge count on notification bell icon in the UI.
    /// Lightweight endpoint — safe to poll frequently.
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var recipientId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _mediator.Send(new GetUnreadCountQuery(recipientId));

        return Ok(ApiResponse<UnreadCountResponse>.Ok(result));
    }

    /// <summary>
    /// Marks a single notification as read with timestamp.
    /// Users can only mark their own notifications — returns 403 otherwise.
    /// Idempotent — no error if notification is already read.
    /// Returns 404 if notification does not exist.
    /// </summary>
    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var recipientId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _mediator.Send(
            new MarkAsReadCommand(id, recipientId));

        return Ok(ApiResponse<string>.Ok(result.Message));
    }

    /// <summary>
    /// Marks all unread notifications as read for the current user.
    /// Uses bulk update for performance — single database operation.
    /// Returns count of notifications that were marked as read.
    /// Safe to call when no unread notifications exist — returns 0 count.
    /// </summary>
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var recipientId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _mediator.Send(
            new MarkAllAsReadCommand(recipientId));

        return Ok(ApiResponse<string>.Ok(result.Message));
    }
}
