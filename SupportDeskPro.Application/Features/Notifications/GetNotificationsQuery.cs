/// <summary>
/// Query model for retrieving paginated notifications
/// for the currently authenticated user.
/// </summary>
using MediatR;
using SupportDeskPro.Application.Features.Tenants.GetTenants;
using SupportDeskPro.Contracts.Notifications;

namespace SupportDeskPro.Application.Features.Notifications.GetNotifications;

public record GetNotificationsQuery(
    Guid RecipientId,
    int Page = 1,
    int PageSize = 20,
    bool? IsRead = null
) : IRequest<PagedResult<NotificationResponse>>;