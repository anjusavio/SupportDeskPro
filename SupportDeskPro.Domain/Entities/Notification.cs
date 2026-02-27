using SupportDeskPro.Domain.Enums;

namespace SupportDeskPro.Domain.Entities
{
    public class Notification
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public Guid RecipientId { get; set; }
        public NotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Guid? TicketId { get; set; }
        public int? TicketNumber { get; set; }          // denormalized — avoids JOIN
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties - define relationships between tables.
        public User Recipient { get; set; } = null!;
        public Ticket? Ticket { get; set; }
    }
}
