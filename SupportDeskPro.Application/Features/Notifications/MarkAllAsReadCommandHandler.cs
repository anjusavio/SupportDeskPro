/// <summary>
/// Handles marking all unread notifications as read for current user.
/// Uses bulk update for performance — single SQL UPDATE instead of
/// loading each notification individually.
/// Returns count of notifications marked as read.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;

namespace SupportDeskPro.Application.Features.Notifications.MarkAllAsRead;

public class MarkAllAsReadCommandHandler
    : IRequestHandler<MarkAllAsReadCommand, MarkAllAsReadResult>
{
    private readonly IApplicationDbContext _db;

    public MarkAllAsReadCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<MarkAllAsReadResult> Handle(
        MarkAllAsReadCommand request,
        CancellationToken cancellationToken)
    {
        // Bulk fetch all unread notifications for this user
        var unreadNotifications = await _db.Notifications
            .Where(n => n.RecipientId == request.RecipientId
                        && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (!unreadNotifications.Any())
            return new MarkAllAsReadResult(
                true, "No unread notifications.", 0);

        // Bulk update — single SaveChanges call for all records
        var now = DateTime.UtcNow;
        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new MarkAllAsReadResult(
            true,
            $"{unreadNotifications.Count} notifications marked as read.",
            unreadNotifications.Count);
    }
}