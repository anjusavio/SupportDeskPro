using static System.Net.Mime.MediaTypeNames;

/// <summary>
/// Tracks the last used ticket number per tenant.
/// Generates sequential human-readable ticket numbers (e.g. #1001, #1002).
/// One record per tenant — incremented on every ticket creation.
/// Not inherited from BaseEntity — no soft delete or audit needed.
/// </summary>
namespace SupportDeskPro.Domain.Entities;

public class TicketNumberSequence
{
    public Guid TenantId { get; set; }
    public int LastNumber { get; set; } = 1000;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Tenant Tenant { get; set; } = null!;
}
