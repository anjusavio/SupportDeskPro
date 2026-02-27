namespace SupportDeskPro.Domain.Entities
{
    // Represents a ticket category within a tenant.
    // Categories can be hierarchical (parent → subcategories) and are used
    // to organize tickets for better classification and reporting.
    public class Category : BaseEntity
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? ParentCategoryId { get; set; }     // NULL = top-level
        public string? ColorHex { get; set; }
        public string? IconName { get; set; }
        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        // Navigation properties - define relationships between tables.
        public Tenant Tenant { get; set; } = null!;
        public Category? ParentCategory { get; set; }   // self-reference
        public ICollection<Category> SubCategories { get; set; } = new List<Category>();
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
