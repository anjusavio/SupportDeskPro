/// <summary>
/// Handles fetching single SLA policy — throws NotFoundException if not found.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.SLAPolicies;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.SLAPolicies.GetSLAPolicyById;

public class GetSLAPolicyByIdQueryHandler
    : IRequestHandler<GetSLAPolicyByIdQuery, SLAPolicyResponse>
{
    private readonly IApplicationDbContext _db;

    public GetSLAPolicyByIdQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<SLAPolicyResponse> Handle(
        GetSLAPolicyByIdQuery request,
        CancellationToken cancellationToken)
    {
        var policy = await _db.SLAPolicies
            .FirstOrDefaultAsync(
                s => s.Id == request.SLAPolicyId,
                cancellationToken)
            ?? throw new NotFoundException(
                "SLAPolicy", request.SLAPolicyId);

        return new SLAPolicyResponse(
            policy.Id,
            policy.Name,
            policy.Priority.ToString(),
            policy.FirstResponseTimeMinutes,
            policy.ResolutionTimeMinutes,
            policy.IsActive,
            policy.CreatedAt);
    }
}