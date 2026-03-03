// Handles agent workload summary — shows ticket counts per agent for Admin dashboard
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Users;
using SupportDeskPro.Domain.Enums;

namespace SupportDeskPro.Application.Features.Users.GetAgentWorkload;

public class GetAgentWorkloadQueryHandler : IRequestHandler<GetAgentWorkloadQuery, List<AgentWorkloadResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetAgentWorkloadQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<AgentWorkloadResponse>> Handle(
        GetAgentWorkloadQuery request,
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        var agents = await _db.Users
            .Where(u => u.Role == UserRole.Agent && u.IsActive)
            .Select(u => new AgentWorkloadResponse(
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                // Open tickets count
                u.AssignedTickets.Count(t =>t.Status == TicketStatus.Open),
                // InProgress tickets count
                u.AssignedTickets.Count(t =>t.Status == TicketStatus.InProgress),
                // Resolved today count
                u.AssignedTickets.Count(t => t.Status == TicketStatus.Resolved &&
                                             t.ResolvedAt.HasValue &&
                                             t.ResolvedAt.Value.Date == today)))
            .ToListAsync(cancellationToken);

        return agents.OrderByDescending(a =>a.OpenTickets + a.InProgressTickets).ToList();
    }
}