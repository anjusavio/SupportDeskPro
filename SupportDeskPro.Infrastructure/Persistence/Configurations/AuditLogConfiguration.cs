using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDeskPro.Domain.Entities;

namespace SupportDeskPro.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(100)
            .IsUnicode(false);

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(100)
            .IsUnicode(false);

        builder.Property(a => a.EntityId)
            .IsRequired()
            .HasMaxLength(100)
            .IsUnicode(false);

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45)
            .IsUnicode(false);

        builder.Property(a => a.UserAgent)
            .HasMaxLength(500);

        // NO foreign keys — intentional
        // AuditLogs must survive entity deletion

        builder.HasIndex(a => new { a.TenantId, a.CreatedAt })
            .HasDatabaseName("IX_AuditLogs_TenantId_CreatedAt");

        builder.HasIndex(a => new { a.EntityType, a.EntityId, a.CreatedAt })
            .HasDatabaseName("IX_AuditLogs_EntityType_EntityId");
    }
}