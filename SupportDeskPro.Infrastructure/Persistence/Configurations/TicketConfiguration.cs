using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDeskPro.Domain.Entities;

namespace SupportDeskPro.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    // EF Core configuration for the Ticket entity.
    // This class defines database mapping, indexes, and relationships
    // to optimize multi-tenant ticket storage and querying.
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(t => t.Description)
            .IsRequired();

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<byte>(); //Store enum value as byte

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(t => t.SLABreachType)
            .HasConversion<byte?>();

        builder.Property(t => t.AISuggestedPriority)
            .HasConversion<byte?>();

        builder.Property(t => t.AICategorizationConfidence)
            .HasPrecision(4, 3); //9.999

        builder.Property(t => t.LastActivityAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()"); //sets current UTC date & time

        
        
        // ── INDEXES (7 total — each for specific query) ───────

        // Agent queue — my assigned open tickets
        builder.HasIndex(t => new { t.TenantId, t.AssignedAgentId, t.Status })
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Tickets_TenantId_AgentId_Status");

        // Admin all tickets view
        builder.HasIndex(t => new { t.TenantId, t.Status, t.Priority })
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Tickets_TenantId_Status_Priority");

        // Customer's own tickets
        builder.HasIndex(t => new { t.TenantId, t.CustomerId, t.Status })
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Tickets_TenantId_CustomerId_Status");

        // SLA background job (runs every 5 min)
        builder.HasIndex(t => new { t.SLAResolutionDueAt, t.IsSLABreached, t.TenantId })
            .HasFilter("[IsDeleted] = 0 AND [Status] <> 4 AND [Status] <> 5")
            .HasDatabaseName("IX_Tickets_SLA_BackgroundJob");

        // Report date range queries
        builder.HasIndex(t => new { t.TenantId, t.CreatedAt })
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Tickets_TenantId_CreatedAt");

        // Auto-close job
        builder.HasIndex(t => new { t.Status, t.LastActivityAt })
            .HasFilter("[IsDeleted] = 0 AND [Status] = 4")
            .HasDatabaseName("IX_Tickets_AutoClose");

        // Human-readable ticket number lookup
        builder.HasIndex(t => new { t.TenantId, t.TicketNumber })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Tickets_TenantId_Number");

        // ── RELATIONSHIPS ─────────────────────────────────────

        builder.HasOne(t => t.Tenant)
            .WithMany(tn => tn.Tickets)
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Category)
            .WithMany(c => c.Tickets)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Customer FK
        builder.HasOne(t => t.Customer)
            .WithMany(u => u.CreatedTickets)
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Agent FK (nullable)
        builder.HasOne(t => t.AssignedAgent)
            .WithMany(u => u.AssignedTickets)
            .HasForeignKey(t => t.AssignedAgentId)
            .OnDelete(DeleteBehavior.Restrict) //You cannot delete a user if tickets reference them
            .IsRequired(false);

        builder.HasOne(t => t.SLAPolicy)
            .WithMany(s => s.Tickets)
            .HasForeignKey(t => t.SLAPolicyId)
            .OnDelete(DeleteBehavior.SetNull) //If SLA policy is deleted, Set SLAPolicyId to NULL on tickets
            .IsRequired(false);
    }
}