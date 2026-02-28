using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Entities;
using SupportDeskPro.Domain.Enums;

namespace SupportDeskPro.Application.Features.Tenants.CreateTenant;

//Handles tenant creation — validates slug uniqueness and auto-creates default TenantSettings
public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, CreateTenantResult>
{
    private readonly IApplicationDbContext _db;

    public CreateTenantCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<CreateTenantResult> Handle(
        CreateTenantCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Check slug is unique
        var slugExists = await _db.Tenants
            .AnyAsync(t => t.Slug == request.Slug.ToLower(),
                cancellationToken);

        if (slugExists)
            return new CreateTenantResult(
                false, "Slug already exists. Choose a different one.");

        // 2. Create tenant
        var tenant = new Tenant
        {
            Name = request.Name.Trim(),
            Slug = request.Slug.ToLower().Trim(),
            PlanType = (PlanType)request.PlanType,
            MaxAgents = request.MaxAgents,
            MaxTickets = request.MaxTickets,
            IsActive = true
        };

        // 3. Auto-create TenantSettings
        var settings = new TenantSettings
        {
            TenantId = tenant.Id,
            TimeZone = "UTC",
            WorkingHoursStart = new TimeOnly(9, 0),
            WorkingHoursEnd = new TimeOnly(18, 0),
            WorkingDays = "1,2,3,4,5",
            AutoCloseAfterDays = 7,
            AllowCustomerSelfRegistration = true
        };

        // 4. Save both
        _db.Tenants.Add(tenant);
        _db.TenantSettings.Add(settings);
        await _db.SaveChangesAsync(cancellationToken);

        return new CreateTenantResult(
            true, "Tenant created successfully.", tenant.Id);
    }
}