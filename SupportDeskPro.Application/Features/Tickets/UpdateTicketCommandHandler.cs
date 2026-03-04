/// <summary>
/// Handles ticket update — validates category is active and
/// belongs to current tenant before updating.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Enums;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Tickets.UpdateTicket;

public class UpdateTicketCommandHandler
    : IRequestHandler<UpdateTicketCommand, UpdateTicketResult>
{
    private readonly IApplicationDbContext _db;

    public UpdateTicketCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UpdateTicketResult> Handle(
        UpdateTicketCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Find ticket
        var ticket = await _db.Tickets
            .FirstOrDefaultAsync(
                t => t.Id == request.TicketId && !t.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        // 2. Cannot update closed ticket
        if (ticket.Status == TicketStatus.Closed)
            throw new BusinessValidationException(
                "Cannot update a closed ticket.");

        // 3. Validate category is active and in this tenant
        var categoryExists = await _db.Categories
            .AnyAsync(c => c.Id == request.CategoryId && c.IsActive,
                cancellationToken);

        if (!categoryExists)
            throw new NotFoundException("Category", request.CategoryId);

        // 4. Update ticket
        ticket.Title = request.Title.Trim();
        ticket.Description = request.Description.Trim();
        ticket.CategoryId = request.CategoryId;
        ticket.Priority = (TicketPriority)request.Priority;
        ticket.LastActivityAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new UpdateTicketResult(true, "Ticket updated successfully.");
    }
}