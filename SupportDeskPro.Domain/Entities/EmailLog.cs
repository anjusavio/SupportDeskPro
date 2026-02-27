using SupportDeskPro.Domain.Enums;

namespace SupportDeskPro.Domain.Entities
{
    public class EmailLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? TenantId { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string TemplateType { get; set; } = string.Empty;
        public Guid? TicketId { get; set; }
        public EmailStatus Status { get; set; } = EmailStatus.Pending;
        public DateTime? SentAt { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; } = 0;
        public DateTime? NextRetryAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
