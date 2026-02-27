using SupportDeskPro.Domain.Enums;

namespace SupportDeskPro.Domain.Entities
{
    // SLA (Service Level Agreement) defines service quality commitments
    // between the service provider and customer, such as response time,
    // resolution time, and escalation rules.
    public class SLAPolicy : BaseEntity
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public TicketPriority Priority { get; set; }
        public int FirstResponseTimeMinutes { get; set; } //How fast agent should reply to customer after ticket is created
        public int ResolutionTimeMinutes { get; set; } //How fast issue should be solved
        public int EscalationTimeMinutes { get; set; }
        public bool IsActive { get; set; } = true;


        // Navigation properties - define relationships between tables.
        public Tenant Tenant { get; set; } = null!;
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
