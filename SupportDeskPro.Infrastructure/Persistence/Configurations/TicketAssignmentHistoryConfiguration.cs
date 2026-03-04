/// <summary>
/// EF Core configuration for TicketAssignmentHistory table.
/// Append-only audit table — records every agent assignment change.
/// </summary>
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDeskPro.Domain.Entities;
using System.Diagnostics;

namespace SupportDeskPro.Infrastructure.Persistence.Configurations;

public class TicketAssignmentHistoryConfiguration
    : IEntityTypeConfiguration<TicketAssignmentHistory>
{
    public void Configure(
        EntityTypeBuilder<TicketAssignmentHistory> builder)
    {
        builder.ToTable("TicketAssignmentHistories");

        builder.HasKey(h => h.Id);

        builder.HasOne(h => h.Ticket)
            .WithMany(t => t.AssignmentHistory)
            .HasForeignKey(h => h.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.AssignedBy)
            .WithMany()
            .HasForeignKey(h => h.AssignedById)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
