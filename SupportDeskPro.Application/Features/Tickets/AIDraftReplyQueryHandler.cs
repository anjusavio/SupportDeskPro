using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Tickets;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Tickets.AIDraftReply;

/// <summary>
/// Drafts an AI reply for the agent based on ticket content
/// and full conversation history.
/// Agent reviews and edits before sending — never auto-sends.
/// </summary>
public class AIDraftReplyQueryHandler
    : IRequestHandler<AIDraftReplyQuery, AIDraftReplyResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IAIDraftReplyService _aiDraftService;

    public AIDraftReplyQueryHandler(
        IApplicationDbContext db,
        IAIDraftReplyService aiDraftService)
    {
        _db = db;
        _aiDraftService = aiDraftService;
    }

    public async Task<AIDraftReplyResponse> Handle(
        AIDraftReplyQuery request,
        CancellationToken cancellationToken)
    {
        // Load ticket with customer info
        var ticket = await _db.Tickets
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(
                t => t.Id == request.TicketId && !t.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        // Load public conversation history (not internal notes)
        var comments = await _db.TicketComments
            .Include(c => c.Author)
            .Where(c => c.TicketId == request.TicketId
                        && !c.IsInternal
                        && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .Select(c => $"{c.Author.FirstName} ({c.Author.Role}): {c.Body}")
            .ToListAsync(cancellationToken);

        // Call AI service
        var draft = await _aiDraftService.DraftReplyAsync(
            ticket.Title,
            ticket.Description,
            ticket.Customer.FirstName,
            comments,
            request.IsInternal,
            cancellationToken);

        return new AIDraftReplyResponse(draft);
    }
}