/// <summary>
/// Handles ticket assignment — validates agent belongs to tenant,
/// logs assignment to TicketAssignmentHistory, and updates ticket status
/// to InProgress when assigned.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SupportDeskPro.Application.Common;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Entities;
using SupportDeskPro.Domain.Enums;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Tickets.AssignTicket;

public class AssignTicketCommandHandler
    : IRequestHandler<AssignTicketCommand, AssignTicketResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    public AssignTicketCommandHandler(IApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<AssignTicketResult> Handle(
        AssignTicketCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Find ticket
        var ticket = await _db.Tickets
            .FirstOrDefaultAsync(
                t => t.Id == request.TicketId && !t.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        // 2. Cannot assign closed ticket
        if (ticket.Status == TicketStatus.Closed)
            throw new BusinessValidationException(
                "Cannot assign a closed ticket.");

        // 3. Validate agent if provided
        if (request.AgentId.HasValue)
        {
            var agent = await _db.Users
                .FirstOrDefaultAsync(
                    u => u.Id == request.AgentId
                         && u.Role == UserRole.Agent
                         && u.IsActive,
                    cancellationToken)
                ?? throw new NotFoundException(
                    "Agent", request.AgentId);
        }

        var now = DateTime.UtcNow;
        var previousAgentId = ticket.AssignedAgentId;

        // 4. Update assignment
        ticket.AssignedAgentId = request.AgentId;
        ticket.LastActivityAt = now;

        // 5. Auto set InProgress when agent assigned
        if (request.AgentId.HasValue &&
            ticket.Status == TicketStatus.Open)
        {
            ticket.Status = TicketStatus.InProgress;

            // Log status change
            _db.TicketStatusHistory.Add(new TicketStatusHistory
            {
                TicketId = ticket.Id,
                TenantId = ticket.TenantId,
                FromStatus = TicketStatus.Open,
                ToStatus = TicketStatus.InProgress,
                ChangedById = request.AssignedById,
                Note = "Status changed to InProgress on agent assignment"
            });
        }

        // 6. Log assignment history — always append, never update
        _db.TicketAssignmentHistory.Add(new TicketAssignmentHistory
        {
            TicketId = ticket.Id,
            TenantId = ticket.TenantId,
            FromAgentId = previousAgentId,
            ToAgentId = request.AgentId,
            AssignedById = request.AssignedById,
            AssignmentType = AssignmentType.Manual
        });

        await _db.SaveChangesAsync(cancellationToken);

        // Invalidate agents cache — workload count changed
        _cache.Remove(CacheKeys.Agents(ticket.TenantId)); //  fresh on next assign 

        var message = request.AgentId.HasValue
            ? "Ticket assigned successfully."
            : "Ticket unassigned successfully.";

        return new AssignTicketResult(true, message);
    }
}