using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDeskPro.Domain.Entities;

namespace SupportDeskPro.Infrastructure.Persistence.Configurations;

// EF Core configuration for the User entity.
// Defines table mapping, property constraints, indexes, and relationships
// to support multi-tenant user management and data integrity
// including roles, authentication data, and tenant isolation.
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(320)
            .IsUnicode(false);      // VARCHAR — emails are ASCII

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500)
            .IsUnicode(false);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<byte>(); // stored as TINYINT -- storing enum value

        builder.Property(u => u.LastLoginIp)
            .HasMaxLength(45)
            .IsUnicode(false);      // IPv6 max length

        builder.Property(u => u.ProfilePictureUrl)
            .HasMaxLength(500);

        builder.Ignore(u => u.FullName); // computed — not stored in DB

        // Unique email per tenant (filtered — excludes deleted users)
        builder.HasIndex(u => new { u.Email, u.TenantId }) //composite unique index
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Users_Email_TenantId"); //Explicit index name in database

        // Agent list per tenant
        builder.HasIndex(u => new { u.TenantId, u.Role })
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Users_TenantId_Role");

        // FK to Tenant
        builder.HasOne(u => u.Tenant)
            .WithMany(t => t.Users)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict)//You cannot delete tenant if users exist
            .IsRequired(false);     // TenantId can be NULL for SuperAdmin

        // RefreshTokens relationship
        builder.HasMany(u => u.RefreshTokens)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade); //If user is deleted, delete their refresh tokens too

        // PasswordResetTokens relationship
        builder.HasMany(u => u.PasswordResetTokens)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}