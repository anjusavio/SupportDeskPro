/// <summary>
/// Handles fetching unread notification count for the current user.
/// Lightweight query — returns single integer for badge display.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Notifications;

namespace SupportDeskPro.Application.Features.Notifications.GetUnreadCount;

public class GetUnreadCountQueryHandler
    : IRequestHandler<GetUnreadCountQuery, UnreadCountResponse>
{
    private readonly IApplicationDbContext _db;

    public GetUnreadCountQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UnreadCountResponse> Handle(
        GetUnreadCountQuery request,
        CancellationToken cancellationToken)
    {
        var count = await _db.Notifications
            .CountAsync(
                n => n.RecipientId == request.RecipientId
                     && !n.IsRead,
                cancellationToken);

        return new UnreadCountResponse(count);
    }
}