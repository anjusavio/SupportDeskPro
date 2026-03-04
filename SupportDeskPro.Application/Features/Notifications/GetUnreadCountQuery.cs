/// <summary>
/// Query model for retrieving unread notification count.
/// Used to display badge count on notification bell icon.
/// </summary>
using MediatR;
using SupportDeskPro.Contracts.Notifications;

namespace SupportDeskPro.Application.Features.Notifications.GetUnreadCount;

public record GetUnreadCountQuery(Guid RecipientId)
    : IRequest<UnreadCountResponse>;