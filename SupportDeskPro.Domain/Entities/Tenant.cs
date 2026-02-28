using SupportDeskPro.Domain.Enums;

namespace SupportDeskPro.Domain.Entities
{
    // Represents a tenant (organization) in the multi-tenant system.
    // Each tenant has its own users, settings, categories, SLA policies,
    // and tickets, ensuring data isolation and scalability.
    public class Tenant : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;//URL-friendly version of a company name (without space). eg: Acme Corporation →acme-corporation
        public PlanType PlanType { get; set; } = PlanType.Free;
        public bool IsActive { get; set; } = true;
        public int MaxAgents { get; set; } = 5;
        public int MaxTickets { get; set; } = 500;

        // Navigation properties - define relationships between tables.
        public TenantSettings? Settings { get; set; }
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<SLAPolicy> SLAPolicies { get; set; } = new List<SLAPolicy>();
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
