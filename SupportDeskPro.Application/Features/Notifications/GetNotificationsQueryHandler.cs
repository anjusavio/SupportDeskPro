/// <summary>
/// Handles fetching paginated notifications for the current user.
/// Supports filtering by read/unread status.
/// Ordered by creation date descending — newest first.
/// Scoped to current tenant via Global Query Filter.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Features.Tenants.GetTenants;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Notifications;

namespace SupportDeskPro.Application.Features.Notifications.GetNotifications;

public class GetNotificationsQueryHandler
    : IRequestHandler<GetNotificationsQuery, PagedResult<NotificationResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetNotificationsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<NotificationResponse>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Notifications
            .Where(n => n.RecipientId == request.RecipientId)
            .AsQueryable();

        if (request.IsRead.HasValue)
            query = query.Where(n => n.IsRead == request.IsRead);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NotificationResponse(
                n.Id,
                n.Type.ToString(),
                n.Title,
                n.Message,
                n.TicketId,
                n.TicketNumber,
                n.IsRead,
                n.ReadAt,
                n.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<NotificationResponse>(
            items, totalCount, request.Page, request.PageSize);
    }
}