using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDeskPro.Domain.Entities;

namespace SupportDeskPro.Infrastructure.Persistence.Configurations;

public class TicketCommentConfiguration : IEntityTypeConfiguration<TicketComment>
{
    public void Configure(EntityTypeBuilder<TicketComment> builder)
    {
        builder.ToTable("TicketComments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Body)
            .IsRequired();

        builder.Property(c => c.SentimentScore)
            .HasPrecision(4, 3);    // -1.000 to +1.000

        builder.Property(c => c.SentimentLabel)
            .HasMaxLength(20)
            .IsUnicode(false);

        // Load full thread
        builder.HasIndex(c => new { c.TicketId, c.CreatedAt })
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_TicketComments_TicketId");

        // Tenant filter + internal notes
        builder.HasIndex(c => new { c.TenantId, c.TicketId, c.IsInternal })
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_TicketComments_TenantId_IsInternal");

        builder.HasOne(c => c.Ticket)
            .WithMany(t => t.Comments)
            .HasForeignKey(c => c.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Author)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}