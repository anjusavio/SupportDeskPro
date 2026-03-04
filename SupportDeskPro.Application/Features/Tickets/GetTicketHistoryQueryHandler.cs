/// <summary>
/// Handles fetching ticket status history — ordered chronologically.
/// Throws NotFoundException if ticket does not exist in current tenant.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Tickets;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Tickets.GetTicketHistory;

public class GetTicketHistoryQueryHandler
    : IRequestHandler<GetTicketHistoryQuery, List<TicketStatusHistoryResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetTicketHistoryQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<TicketStatusHistoryResponse>> Handle(
        GetTicketHistoryQuery request,
        CancellationToken cancellationToken)
    {
        // Verify ticket exists in current tenant
        var ticketExists = await _db.Tickets
            .AnyAsync(t => t.Id == request.TicketId && !t.IsDeleted,
                cancellationToken);

        if (!ticketExists)
            throw new NotFoundException("Ticket", request.TicketId);

        return await _db.TicketStatusHistory
            .Include(h => h.ChangedBy)
            .Where(h => h.TicketId == request.TicketId)
            .OrderBy(h => h.CreatedAt)
            .Select(h => new TicketStatusHistoryResponse(
                h.Id,
                h.FromStatus.HasValue
                    ? h.FromStatus.ToString()
                    : null,
                h.ToStatus.ToString(),
                h.ChangedBy.FirstName + " " + h.ChangedBy.LastName,
                h.Note,
                h.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}