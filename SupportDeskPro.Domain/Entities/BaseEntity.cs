namespace SupportDeskPro.Domain.Entities
{
    //every other entity inherits from BaseEntity.
    //It contains the common columns every table has
    public abstract class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid(); //// Works perfectly in distributed systems
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Guid? CreatedBy { get; set; } // nullable ← can be null for system actions like Background job/sent email, etc.
        public Guid? UpdatedBy { get; set; }

    }
}
