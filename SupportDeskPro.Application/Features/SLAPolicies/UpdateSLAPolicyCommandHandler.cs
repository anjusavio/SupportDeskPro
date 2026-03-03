/// <summary>
/// Handles SLA policy update — validates resolution time
/// is always greater than first response time.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.SLAPolicies.UpdateSLAPolicy;

public class UpdateSLAPolicyCommandHandler
    : IRequestHandler<UpdateSLAPolicyCommand, UpdateSLAPolicyResult>
{
    private readonly IApplicationDbContext _db;

    public UpdateSLAPolicyCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UpdateSLAPolicyResult> Handle(
        UpdateSLAPolicyCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate resolution > first response
        if (request.ResolutionTimeMinutes <= request.FirstResponseTimeMinutes)
            throw new BusinessValidationException(
                "Resolution time must be greater than first response time.");

        // 2. Find policy
        var policy = await _db.SLAPolicies
            .FirstOrDefaultAsync(
                s => s.Id == request.SLAPolicyId,
                cancellationToken)
            ?? throw new NotFoundException(
                "SLAPolicy", request.SLAPolicyId);

        // 3. Update
        policy.Name = request.Name.Trim();
        policy.FirstResponseTimeMinutes = request.FirstResponseTimeMinutes;
        policy.ResolutionTimeMinutes = request.ResolutionTimeMinutes;

        await _db.SaveChangesAsync(cancellationToken);

        return new UpdateSLAPolicyResult(
            true, "SLA policy updated successfully.");
    }
}