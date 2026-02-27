using SupportDeskPro.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportDeskPro.Domain.Entities
{
    public class Ticket : BaseEntity
    {
        public Guid TenantId { get; set; }
        public int TicketNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TicketStatus Status { get; set; } = TicketStatus.Open;
        public TicketPriority Priority { get; set; } = TicketPriority.Medium;
        public Guid CategoryId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid? AssignedAgentId { get; set; }
        public Guid? SLAPolicyId { get; set; }

        // SLA Tracking
        public DateTime? SLAFirstResponseDueAt { get; set; }
        public DateTime? SLAResolutionDueAt { get; set; }
        public DateTime? FirstResponseAt { get; set; }
        public bool IsSLABreached { get; set; } = false;
        public DateTime? SLABreachedAt { get; set; }
        public SLABreachType? SLABreachType { get; set; }

        // AI fields
        public Guid? AISuggestedCategoryId { get; set; }
        public TicketPriority? AISuggestedPriority { get; set; }
        public decimal? AICategorizationConfidence { get; set; }

        // Lifecycle
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        // Soft delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }

        // Navigation properties - define relationships between tables.
        public Tenant Tenant { get; set; } = null!;
        public Category Category { get; set; } = null!;
        public User Customer { get; set; } = null!;
        public User? AssignedAgent { get; set; }
        public SLAPolicy? SLAPolicy { get; set; }
        public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
        public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
        public ICollection<TicketStatusHistory> StatusHistory { get; set; } = new List<TicketStatusHistory>();
        public ICollection<TicketAssignmentHistory> AssignmentHistory { get; set; } = new List<TicketAssignmentHistory>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
