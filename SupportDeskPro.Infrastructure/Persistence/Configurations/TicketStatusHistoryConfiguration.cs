/// <summary>
/// EF Core configuration for TicketStatusHistory table.
/// Append-only audit table — records every ticket status change.
/// </summary>
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDeskPro.Domain.Entities;

namespace SupportDeskPro.Infrastructure.Persistence.Configurations;

public class TicketStatusHistoryConfiguration : IEntityTypeConfiguration<TicketStatusHistory>
{
    public void Configure(
        EntityTypeBuilder<TicketStatusHistory> builder)
    {
        // ← Explicitly set table name to match DB
        builder.ToTable("TicketStatusHistories");

        builder.HasKey(h => h.Id);

        builder.HasOne(h => h.Ticket)
            .WithMany(t => t.StatusHistory)
            .HasForeignKey(h => h.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.ChangedBy)
            .WithMany()
            .HasForeignKey(h => h.ChangedById)
            .OnDelete(DeleteBehavior.NoAction);
    }
}