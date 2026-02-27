namespace SupportDeskPro.Domain.Entities
{
    // Tenant settings entity that controls operational behavior
    // such as working hours, auto-close rules, and self-registration.
    // These settings are isolated per tenant for multi-tenant support.
    public class TenantSettings : BaseEntity
    {
        public Guid TenantId { get; set; }
        public string TimeZone { get; set; } = "UTC";
        public TimeOnly WorkingHoursStart { get; set; } = new TimeOnly(9, 0);
        public TimeOnly WorkingHoursEnd { get; set; } = new TimeOnly(18, 0);
        public string WorkingDays { get; set; } = "1,2,3,4,5";
        public int AutoCloseAfterDays { get; set; } = 7;
        public bool AllowCustomerSelfRegistration { get; set; } = true;

        // Navigation property - define relationships between tables.
        public Tenant Tenant { get; set; } = null!;
    }
}
