namespace SupportDeskPro.Application.Interfaces;

/// <summary>
/// Finds semantically similar resolved tickets using Claude AI.
/// Takes pre-filtered candidate tickets from SQL and scores
/// each one for similarity using Claude's language understanding.
/// Returns top 3 most similar with a score between 0 and 1.
/// </summary>
public interface IAISimilarTicketService
{
    Task<List<SimilarTicketAIResult>> FindSimilarTicketsAsync(
        string currentTitle,
        string currentDescription,
        List<CandidateTicket> candidates,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// If returning the last comment from the ticket may not be the atucal thing helped to solve the issue,
    /// So need to read the entire history of chat.
    /// Sends the full public conversation to Claude Haiku.
    /// Claude reads every message, identifies what actually solved the issue,
    /// and returns 1-3 concise actionable sentences.
    /// </summary>

    Task<string> ExtractResolutionAsync(
       string ticketTitle,
       List<string> conversation,
       CancellationToken cancellationToken = default);
}

public record CandidateTicket(
    Guid Id,
    int TicketNumber,
    string Title,
    string Description,
    List<string> Conversation);

public record SimilarTicketAIResult(
    Guid Id,
    double SimilarityScore,
    string Reason);