namespace SupportDeskPro.Domain.Entities
{
    // Audit log entity used for tracking system and user actions.
    public class AuditLog
    {
        // No BaseEntity — no FKs intentionally
        // Must survive entity deletion
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? TenantId { get; set; }
        public Guid? UserId { get; set; }               // NULL = system action
        public string Action { get; set; } = string.Empty;      // "Ticket.Created"
        public string EntityType { get; set; } = string.Empty;  // "Ticket"
        public string EntityId { get; set; } = string.Empty;    // GUID as string
        public string? OldValues { get; set; }          // JSON snapshot before
        public string? NewValues { get; set; }          // JSON snapshot after
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
