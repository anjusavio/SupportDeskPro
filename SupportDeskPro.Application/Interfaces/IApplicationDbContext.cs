using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Domain.Entities;

namespace SupportDeskPro.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<TenantSettings> TenantSettings { get; }
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<Category> Categories { get; }
    DbSet<SLAPolicy> SLAPolicies { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<TicketComment> TicketComments { get; }
    DbSet<TicketAttachment> TicketAttachments { get; }
    DbSet<TicketStatusHistory> TicketStatusHistories { get; }
    DbSet<TicketAssignmentHistory> TicketAssignmentHistories { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<EmailLog> EmailLogs { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<BackgroundJobLog> BackgroundJobLogs { get; }
    DbSet<AIInteractionLog> AIInteractionLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}