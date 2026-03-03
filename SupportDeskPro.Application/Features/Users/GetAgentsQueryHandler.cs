// Handles fetching active agents — used in ticket assignment dropdown
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Users;
using SupportDeskPro.Domain.Enums;

namespace SupportDeskPro.Application.Features.Users.GetAgents;

public class GetAgentsQueryHandler
    : IRequestHandler<GetAgentsQuery, List<AgentSummaryResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetAgentsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<AgentSummaryResponse>> Handle(
        GetAgentsQuery request,
        CancellationToken cancellationToken)
    {
        return await _db.Users
            .Where(u => u.Role == UserRole.Agent && u.IsActive)
            .OrderBy(u => u.FirstName)
            .Select(u => new AgentSummaryResponse(
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email))
            .ToListAsync(cancellationToken);
    }
}