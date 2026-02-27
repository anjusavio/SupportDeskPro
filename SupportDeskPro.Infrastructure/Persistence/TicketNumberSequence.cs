namespace SupportDeskPro.Infrastructure.Persistence;

// Not a domain entity — infrastructure concern only
public class TicketNumberSequence
{
    public Guid TenantId { get; set; }
    public int LastNumber { get; set; } = 1000;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}