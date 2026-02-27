using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDeskPro.Domain.Entities;

namespace SupportDeskPro.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Slug)
            .IsRequired()
            .HasMaxLength(100)
            .IsUnicode(false);      // VARCHAR not NVARCHAR

        builder.Property(t => t.PlanType)
            .IsRequired()
            .HasConversion<byte>(); // stored as TINYINT

        builder.Property(t => t.MaxAgents)
            .IsRequired()
            .HasDefaultValue(5);

        builder.Property(t => t.MaxTickets)
            .IsRequired()
            .HasDefaultValue(500);

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Unique constraint on Slug
        builder.HasIndex(t => t.Slug)
            .IsUnique()
            .HasDatabaseName("UX_Tenants_Slug");

        // Index for active tenant list
        builder.HasIndex(t => t.IsActive)
            .HasDatabaseName("IX_Tenants_IsActive");

        // Relationships
        builder.HasOne(t => t.Settings)
            .WithOne(s => s.Tenant)
            .HasForeignKey<TenantSettings>(s => s.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}