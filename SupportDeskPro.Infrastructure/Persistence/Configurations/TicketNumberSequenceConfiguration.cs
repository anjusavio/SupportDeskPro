/// <summary>
/// EF Core configuration for TicketNumberSequences table.
/// TenantId is the primary key — one sequence record per tenant.
/// Incremented on every ticket creation to generate sequential ticket numbers.
/// </summary>
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDeskPro.Domain.Entities;

namespace SupportDeskPro.Infrastructure.Persistence.Configurations;

public class TicketNumberSequenceConfiguration
    : IEntityTypeConfiguration<TicketNumberSequence>
{
    public void Configure(
        EntityTypeBuilder<TicketNumberSequence> builder)
    {
        builder.ToTable("TicketNumberSequences");

        // TenantId is primary key 
        builder.HasKey(t => t.TenantId);

        builder.Property(t => t.LastNumber)
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .IsRequired();

        // One sequence per tenant
        builder.HasOne(t => t.Tenant)
            .WithOne()
            .HasForeignKey<TicketNumberSequence>(t => t.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}