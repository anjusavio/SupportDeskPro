/// <summary>
/// Handles fetching ticket comments — filters internal notes
/// from customer view. Ordered chronologically oldest first.
/// Throws NotFoundException if ticket does not exist in current tenant.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Comments;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Comments.GetComments;

public class GetCommentsQueryHandler
    : IRequestHandler<GetCommentsQuery, List<CommentResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetCommentsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<CommentResponse>> Handle(
        GetCommentsQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Verify ticket exists in current tenant
        var ticketExists = await _db.Tickets
            .AnyAsync(t => t.Id == request.TicketId
                          && !t.IsDeleted,
                cancellationToken);

        if (!ticketExists)
            throw new NotFoundException("Ticket", request.TicketId);

        // 2. Build query
        var query = _db.TicketComments
            .Include(c => c.Author)
            .Where(c => c.TicketId == request.TicketId
                        && !c.IsDeleted)
            .AsQueryable();

        // 3. Customers cannot see internal notes
        if (request.IsCustomer)
            query = query.Where(c => !c.IsInternal);

        // 4. Return ordered chronologically
        return await query
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentResponse(
                c.Id,
                c.TicketId,
                c.AuthorId,
                c.Author.FirstName + " " + c.Author.LastName,
                c.Author.Role.ToString(),
                c.Body,
                c.IsInternal,
                c.IsEdited,
                c.EditedAt,
                c.SentimentScore,
                c.SentimentLabel,
                c.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}