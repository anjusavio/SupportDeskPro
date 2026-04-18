/// <summary>
/// Handles fetching full ticket detail with all related data.
/// Throws NotFoundException if ticket does not exist in current tenant.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Tickets;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Tickets.GetTicketById;

public class GetTicketByIdQueryHandler
    : IRequestHandler<GetTicketByIdQuery, TicketDetailResponse>
{
    private readonly IApplicationDbContext _db;

    public GetTicketByIdQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<TicketDetailResponse> Handle(
        GetTicketByIdQuery request,
        CancellationToken cancellationToken)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Category)
            .Include(t => t.Customer)
            .Include(t => t.AssignedAgent)
            .Include(t => t.AISuggestedCategory)
            .Include(t => t.Attachments            
                .Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(
                t => t.Id == request.TicketId && !t.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        return new TicketDetailResponse(
            ticket.Id,
            ticket.TicketNumber,
            ticket.Title,
            ticket.Description,
            ticket.Status.ToString(),
            ticket.Priority.ToString(),
            ticket.CategoryId,
            ticket.Category.Name,
            ticket.CustomerId,
            ticket.Customer.FirstName + " " + ticket.Customer.LastName,
            ticket.Customer.Email,
            ticket.AssignedAgentId,
            ticket.AssignedAgent != null
                ? ticket.AssignedAgent.FirstName + " " + ticket.AssignedAgent.LastName
                : null,
            ticket.AssignedAgent?.Email,
            ticket.SLAFirstResponseDueAt,
            ticket.SLAResolutionDueAt,
            ticket.FirstResponseAt,
            ticket.ResolvedAt,
            ticket.ClosedAt,
            ticket.IsSLABreached,
            ticket.SLABreachedAt,
            ticket.AISuggestedCategory?.Name,
            ticket.AISuggestedPriority?.ToString(),
            ticket.AICategorizationConfidence,
            ticket.LastActivityAt,
            ticket.CreatedAt,
            ticket.Attachments
            .Where(a => a.CommentId == null) // ticket-level only, not comment attachments
            .Select(a => new TicketAttachmentResponse(
                a.Id,
                a.OriginalFileName,
                a.FileSizeBytes,
                a.ContentType,
                a.BlobUrl,
                a.CreatedAt))
            .ToList()
            );
    }
}