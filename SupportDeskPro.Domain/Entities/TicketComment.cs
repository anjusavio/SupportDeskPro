namespace SupportDeskPro.Domain.Entities
{
    public class TicketComment : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid TicketId { get; set; }
        public Guid AuthorId { get; set; }
        public string Body { get; set; } = string.Empty;
        public bool IsInternal { get; set; } = false;   // true = agent-only note
        public bool IsEdited { get; set; } = false;
        public DateTime? EditedAt { get; set; }

        // AI Sentiment
        public decimal? SentimentScore { get; set; }    // -1.0 to +1.0
        public string? SentimentLabel { get; set; }     // Positive, Neutral, Negative

        // Soft delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }

        // Navigation properties - define relationships between tables.
        public Tenant Tenant { get; set; } = null!;
        public Ticket Ticket { get; set; } = null!;
        public User Author { get; set; } = null!;
        public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    }
}
