/// <summary>
/// Command model for marking all unread notifications as read
/// for the currently authenticated user.
/// </summary>
using MediatR;

namespace SupportDeskPro.Application.Features.Notifications.MarkAllAsRead;

public record MarkAllAsReadCommand(
    Guid RecipientId
) : IRequest<MarkAllAsReadResult>;

public record MarkAllAsReadResult(
    bool Success,
    string Message,
    int MarkedCount
);