using SupportDeskPro.Domain.Enums;

namespace SupportDeskPro.Domain.Entities
{
    public class AIInteractionLog
    {
        // No BaseEntity — no FKs intentionally
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? TenantId { get; set; }
        public Guid? TicketId { get; set; }
        public Guid? UserId { get; set; }
        public AIFeatureType FeatureType { get; set; }
        public string Model { get; set; } = string.Empty;      // "gpt-4o-mini"
        public int PromptTokens { get; set; } = 0;
        public int CompletionTokens { get; set; } = 0;
        public int TotalTokens { get; set; } = 0;
        public decimal? EstimatedCostUSD { get; set; }
        public string? PromptSummary { get; set; }
        public string? ResponseSummary { get; set; }
        public int? DurationMs { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
