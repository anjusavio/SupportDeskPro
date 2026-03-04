/// <summary>
/// Handles ticket creation — the most complex operation in the system.
/// Responsibilities:
/// 1. Validate category belongs to tenant
/// 2. Generate unique ticket number using TicketNumberSequences
/// 3. Find and assign SLA policy based on priority
/// 4. Calculate SLA due dates
/// 5. Create ticket record
/// 6. Log initial status history
/// 7. Update last activity timestamp
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Entities;
using SupportDeskPro.Domain.Enums;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Tickets.CreateTicket;

public class CreateTicketCommandHandler
    : IRequestHandler<CreateTicketCommand, CreateTicketResult>
{
    private readonly IApplicationDbContext _db;

    public CreateTicketCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<CreateTicketResult> Handle(
        CreateTicketCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate category belongs to this tenant and is active
        var category = await _db.Categories
            .FirstOrDefaultAsync(
                c => c.Id == request.CategoryId && c.IsActive,
                cancellationToken)
            ?? throw new NotFoundException(
                "Category", request.CategoryId);

        // 2. Validate priority range
        if (request.Priority < 1 || request.Priority > 4)
            throw new BusinessValidationException(
                "Priority must be between 1 (Low) and 4 (Critical).");

        // 3. Generate ticket number
        var ticketNumber = await GenerateTicketNumberAsync(
            request.TenantId, cancellationToken);

        // 4. Find matching SLA policy by priority
        var priority = (TicketPriority)request.Priority;
        var slaPolicy = await _db.SLAPolicies
            .FirstOrDefaultAsync(
                s => s.Priority == priority && s.IsActive,
                cancellationToken);

        // 5. Calculate SLA due dates if policy found
        var now = DateTime.UtcNow;
        DateTime? firstResponseDueAt = slaPolicy != null
            ? now.AddMinutes(slaPolicy.FirstResponseTimeMinutes)
            : null;

        DateTime? resolutionDueAt = slaPolicy != null
            ? now.AddMinutes(slaPolicy.ResolutionTimeMinutes)
            : null;

        // 6. Create ticket
        var ticket = new Ticket
        {
            TenantId = request.TenantId,
            TicketNumber = ticketNumber,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Status = TicketStatus.Open,
            Priority = priority,
            CategoryId = request.CategoryId,
            CustomerId = request.CustomerId,
            SLAPolicyId = slaPolicy?.Id,
            SLAFirstResponseDueAt = firstResponseDueAt,
            SLAResolutionDueAt = resolutionDueAt,
            LastActivityAt = now
        };

        _db.Tickets.Add(ticket);

        // 7. Log initial status history
        var statusHistory = new TicketStatusHistory
        {
            TicketId = ticket.Id,
            TenantId = request.TenantId,
            FromStatus = null,      // null = first status ever
            ToStatus = TicketStatus.Open,
            ChangedById = request.CustomerId,
            Note = "Ticket created"
        };

        _db.TicketStatusHistory.Add(statusHistory);

        await _db.SaveChangesAsync(cancellationToken);

        return new CreateTicketResult(
            true,
            "Ticket created successfully.",
            ticket.Id,
            ticket.TicketNumber);
    }

    /// <summary>
    /// Generates next sequential ticket number for the tenant.
    /// Creates TicketNumberSequence record if first ticket for tenant.
    /// </summary>
    private async Task<int> GenerateTicketNumberAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var sequence = await _db.TicketNumberSequences
            .FirstOrDefaultAsync(
                s => s.TenantId == tenantId,
                cancellationToken);

        if (sequence == null)
        {
            // First ticket for this tenant — start at 1001
            sequence = new TicketNumberSequence
            {
                TenantId = tenantId,
                LastNumber = 1001
            };
            _db.TicketNumberSequences.Add(sequence);
        }
        else
        {
            sequence.LastNumber++;
        }

        return sequence.LastNumber;
    }
}