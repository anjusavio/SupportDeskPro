/// <summary>
/// Handles SLA policy activation/deactivation.
/// Deactivated policies excluded from new ticket SLA assignments.
/// Existing ticket SLA deadlines are not affected.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.SLAPolicies.UpdateSLAPolicyStatus;

public class UpdateSLAPolicyStatusCommandHandler
    : IRequestHandler<UpdateSLAPolicyStatusCommand, UpdateSLAPolicyStatusResult>
{
    private readonly IApplicationDbContext _db;

    public UpdateSLAPolicyStatusCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UpdateSLAPolicyStatusResult> Handle(
        UpdateSLAPolicyStatusCommand request,
        CancellationToken cancellationToken)
    {
        var policy = await _db.SLAPolicies
            .FirstOrDefaultAsync(
                s => s.Id == request.SLAPolicyId,
                cancellationToken)
            ?? throw new NotFoundException(
                "SLAPolicy", request.SLAPolicyId);

        policy.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);

        var status = request.IsActive ? "activated" : "deactivated";
        return new UpdateSLAPolicyStatusResult(
            true, $"SLA policy {status} successfully.");
    }
}