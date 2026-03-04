/// <summary>
/// Handles ticket status change — logs every transition to TicketStatusHistory.
/// Sets ResolvedAt when status changes to Resolved.
/// Sets ClosedAt when status changes to Closed.
/// Prevents invalid status transitions.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Entities;
using SupportDeskPro.Domain.Enums;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Tickets.UpdateTicketStatus;

public class UpdateTicketStatusCommandHandler
    : IRequestHandler<UpdateTicketStatusCommand, UpdateTicketStatusResult>
{
    private readonly IApplicationDbContext _db;

    public UpdateTicketStatusCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UpdateTicketStatusResult> Handle(
        UpdateTicketStatusCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate status range
        if (request.Status < 1 || request.Status > 4)
            throw new BusinessValidationException(
                "Status must be 1=Open, 2=InProgress, 3=Resolved, 4=Closed.");

        // 2. Find ticket
        var ticket = await _db.Tickets
            .FirstOrDefaultAsync(
                t => t.Id == request.TicketId && !t.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        var newStatus = (TicketStatus)request.Status;

        // 3. Prevent re-setting same status
        if (ticket.Status == newStatus)
            throw new BusinessValidationException(
                $"Ticket is already in {newStatus} status.");

        // 4. Prevent reopening closed ticket
        if (ticket.Status == TicketStatus.Closed)
            throw new BusinessValidationException(
                "Cannot change status of a closed ticket.");

        var previousStatus = ticket.Status;
        var now = DateTime.UtcNow;

        // 5. Update lifecycle timestamps
        if (newStatus == TicketStatus.Resolved)
            ticket.ResolvedAt = now;

        if (newStatus == TicketStatus.Closed)
            ticket.ClosedAt = now;

        // 6. Update ticket status
        ticket.Status = newStatus;
        ticket.LastActivityAt = now;

        // 7. Log status change to history — always append, never update
        var history = new TicketStatusHistory
        {
            TicketId = ticket.Id,
            TenantId = ticket.TenantId,
            FromStatus = previousStatus,
            ToStatus = newStatus,
            ChangedById = request.ChangedById,
            Note = request.Note?.Trim()
        };

        _db.TicketStatusHistory.Add(history);
        await _db.SaveChangesAsync(cancellationToken);

        return new UpdateTicketStatusResult(
            true, $"Ticket status updated to {newStatus} successfully.");
    }
}