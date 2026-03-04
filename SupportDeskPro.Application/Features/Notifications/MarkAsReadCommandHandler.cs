/// <summary>
/// Handles marking a single notification as read.
/// Validates notification belongs to the requesting user.
/// Skips update if notification is already read — no error thrown.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Notifications.MarkAsRead;

public class MarkAsReadCommandHandler
    : IRequestHandler<MarkAsReadCommand, MarkAsReadResult>
{
    private readonly IApplicationDbContext _db;

    public MarkAsReadCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<MarkAsReadResult> Handle(
        MarkAsReadCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Find notification — validate belongs to requesting user
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(
                n => n.Id == request.NotificationId,
                cancellationToken)
            ?? throw new NotFoundException(
                "Notification", request.NotificationId);

        // 2. Ensure user can only mark their own notifications
        if (notification.RecipientId != request.RecipientId)
            throw new ForbiddenException(
                "You can only mark your own notifications as read.");

        // 3. Skip if already read — idempotent operation
        if (notification.IsRead)
            return new MarkAsReadResult(
                true, "Notification already marked as read.");

        // 4. Mark as read
        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new MarkAsReadResult(
            true, "Notification marked as read.");
    }
}