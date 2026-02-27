using SupportDeskPro.Domain.Enums;

namespace SupportDeskPro.Domain.Entities
{
    // Records historical ticket assignment events for audit and tracking.
    // Captures who assigned the ticket, previous and new agents, and assignment type
    // to maintain a complete assignment history.
    public class TicketAssignmentHistory
    {
        // Immutable — no BaseEntity, no UpdatedAt
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public Guid TicketId { get; set; }
        public Guid AssignedById { get; set; }
        public Guid? FromAgentId { get; set; }          // NULL = was unassigned
        public Guid? ToAgentId { get; set; }            // NULL = being unassigned
        public AssignmentType AssignmentType { get; set; } = AssignmentType.Manual;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties - define relationships between tables.
        public Ticket Ticket { get; set; } = null!;
        public User AssignedBy { get; set; } = null!;
        public User? FromAgent { get; set; }
        public User? ToAgent { get; set; }
    }
}
