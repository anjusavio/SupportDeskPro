/// <summary>
/// Handles comment creation with the following responsibilities:
/// 1. Validate ticket exists and is not closed
/// 2. Prevent customers from posting internal notes
/// 3. Track first agent response for SLA calculation
/// 4. Update ticket LastActivityAt timestamp
/// 5. Create comment record
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Entities;
using SupportDeskPro.Domain.Enums;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Comments.CreateComment;

public class CreateCommentCommandHandler
    : IRequestHandler<CreateCommentCommand, CreateCommentResult>
{
    private readonly IApplicationDbContext _db;

    public CreateCommentCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<CreateCommentResult> Handle(
        CreateCommentCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Find ticket
        var ticket = await _db.Tickets
            .FirstOrDefaultAsync(
                t => t.Id == request.TicketId && !t.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        // 2. Cannot comment on closed ticket
        if (ticket.Status == TicketStatus.Closed)
            throw new BusinessValidationException("Cannot add comments to a closed ticket.");

        // 3. Customers cannot post internal notes
        if (request.IsInternal && request.AuthorRole == UserRole.Customer.ToString())
            throw new ForbiddenException(
                "Customers cannot post internal notes.");

        var now = DateTime.UtcNow;

        // 4. Track first agent response for SLA
        var isAgentOrAdmin = request.AuthorRole == UserRole.Agent.ToString()
                             || request.AuthorRole == UserRole.Admin.ToString();

        if (isAgentOrAdmin && !request.IsInternal && ticket.FirstResponseAt == null)
        {
            // First public agent response — SLA first response clock stops
            ticket.FirstResponseAt = now;
        }

        // 5. Update last activity timestamp
        ticket.LastActivityAt = now;

        // 6. Create comment
        var comment = new TicketComment
        {
            TenantId = ticket.TenantId,
            TicketId = request.TicketId,
            AuthorId = request.AuthorId,
            Body = request.Body.Trim(),
            IsInternal = request.IsInternal
        };

        _db.TicketComments.Add(comment);
        await _db.SaveChangesAsync(cancellationToken);

        return new CreateCommentResult(true, "Comment posted successfully.", comment.Id);
    }
}