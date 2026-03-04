/// <summary>
/// Response models for notification list and unread count views.
/// </summary>
namespace SupportDeskPro.Contracts.Notifications;

public record NotificationResponse(
    Guid Id,
    string Type,
    string Title,
    string Message,
    Guid? TicketId,
    int? TicketNumber,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt
);

public record UnreadCountResponse(
    int Count
);