using SupportDeskPro.Domain.Enums;

namespace SupportDeskPro.Domain.Entities
{
    public class TicketStatusHistory
    {
        // Immutable — no BaseEntity, no UpdatedAt
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public Guid TicketId { get; set; }
        public Guid ChangedById { get; set; }
        public TicketStatus? FromStatus { get; set; }   // NULL on ticket creation
        public TicketStatus ToStatus { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties - define relationships between tables.
        public Ticket Ticket { get; set; } = null!;
        public User ChangedBy { get; set; } = null!;
    }
}
