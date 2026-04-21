namespace SupportDeskPro.Domain.Entities
{
    public class TicketAttachment : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid TicketId { get; set; }
        public Guid? CommentId { get; set; }            // NULL = ticket-level attachment
        public Guid UploadedById { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string BlobUrl { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string ContentType { get; set; } = string.Empty;


        // Navigation properties - define relationships between tables.
        public Tenant Tenant { get; set; } = null!;
        public Ticket Ticket { get; set; } = null!;
        public TicketComment? Comment { get; set; }
        public User UploadedBy { get; set; } = null!;
    }
}
