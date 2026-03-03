// Handles agent invitation — creates user with temp password and sends welcome email
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Entities;
using SupportDeskPro.Domain.Enums;

namespace SupportDeskPro.Application.Features.Users.InviteAgent;

public class InviteAgentCommandHandler
    : IRequestHandler<InviteAgentCommand, InviteAgentResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;

    public InviteAgentCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IEmailService emailService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
    }

    public async Task<InviteAgentResult> Handle(
        InviteAgentCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Check email not already in use in this tenant
        var exists = await _db.Users
            .AnyAsync(u => u.Email == request.Email.ToLower(),
                cancellationToken);

        if (exists)
            return new InviteAgentResult(
                false, "An agent with this email already exists.");

        // 2. Generate temporary password
        var tempPassword = GenerateTempPassword();
        var passwordHash = _passwordHasher.Hash(tempPassword);

        // 3. Create agent user
        var agent = new User
        {
            TenantId = request.TenantId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.ToLower().Trim(),
            PasswordHash = passwordHash,
            Role = UserRole.Agent,
            IsActive = true,
            IsEmailVerified = true   // Admin-invited agents skip verification
        };

        _db.Users.Add(agent);
        await _db.SaveChangesAsync(cancellationToken);

        // 4. Send welcome email with temp password
        await _emailService.SendAgentInviteAsync(
            agent.Email,
            agent.FirstName,
            tempPassword);

        return new InviteAgentResult(
            true, "Agent invited successfully.", agent.Id);
    }

    private static string GenerateTempPassword()
    {
        // Generates password like: Temp@8472
        var number = new Random().Next(1000, 9999);
        return $"Temp@{number}";
    }
}