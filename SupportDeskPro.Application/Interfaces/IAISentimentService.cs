namespace SupportDeskPro.Application.Interfaces;

/// <summary>
/// Detects customer sentiment from ticket content.
/// Helps agents understand emotional state before replying.
/// Three levels: Frustrated, Concerned, Neutral.
/// </summary>
public interface IAISentimentService
{
    Task<SentimentResult> AnalyseSentimentAsync(
        string ticketDescription,
        List<string> customerMessages,
        CancellationToken cancellationToken = default);
}

public record SentimentResult(
    string Level,
    string Label,
    decimal Confidence,
    string[] TriggerPhrases,
    string AgentAdvice);