/// <summary>
/// Command model for marking a single notification as read.
/// RecipientId used to ensure users can only mark their own notifications.
/// </summary>
using MediatR;

namespace SupportDeskPro.Application.Features.Notifications.MarkAsRead;

public record MarkAsReadCommand(
    Guid NotificationId,
    Guid RecipientId
) : IRequest<MarkAsReadResult>;

public record MarkAsReadResult(
    bool Success,
    string Message
);