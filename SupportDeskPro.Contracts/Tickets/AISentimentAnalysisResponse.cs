namespace SupportDeskPro.Contracts.Tickets;

/// <summary>
/// Sentiment analysis result for a support ticket.
/// Used by agents to identify customer emotional state before replying.
/// </summary>
public record AISentimentAnalysisResponse(
    string Level,           // Frustrated | Concerned | Neutral
    string Label,           // Human readable label shown in badge
    decimal Confidence,     // 0 to 1 — how confident Claude is
    string[] TriggerPhrases, // exact phrases that indicate sentiment
    string AgentAdvice      // what agent should do based on sentiment
);