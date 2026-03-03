/// <summary>
/// Handles SLA policy creation — enforces one active policy per priority per tenant.
/// Priority values: 1=Low, 2=Medium, 3=High, 4=Critical.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Entities;
using SupportDeskPro.Domain.Enums;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.SLAPolicies.CreateSLAPolicy;

public class CreateSLAPolicyCommandHandler
    : IRequestHandler<CreateSLAPolicyCommand, CreateSLAPolicyResult>
{
    private readonly IApplicationDbContext _db;

    public CreateSLAPolicyCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<CreateSLAPolicyResult> Handle(
        CreateSLAPolicyCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate priority range
        if (request.Priority < 1 || request.Priority > 4)
            throw new BusinessValidationException(
                "Priority must be between 1 (Low) and 4 (Critical).");

        // 2. Check one active policy per priority per tenant
        var exists = await _db.SLAPolicies
            .AnyAsync(s => (int)s.Priority == request.Priority
                           && s.IsActive,
                cancellationToken);

        if (exists)
            throw new ConflictException($"An active SLA policy for this priority already exists.");

        // 3. Create policy
        var policy = new SLAPolicy
        {
            TenantId = request.TenantId,
            Name = request.Name.Trim(),
            Priority = (TicketPriority)request.Priority,
            FirstResponseTimeMinutes = request.FirstResponseTimeMinutes,
            ResolutionTimeMinutes = request.ResolutionTimeMinutes,
            IsActive = true
        };

        _db.SLAPolicies.Add(policy);
        await _db.SaveChangesAsync(cancellationToken);

        return new CreateSLAPolicyResult(
            true, "SLA policy created successfully.", policy.Id);
    }
}