namespace SupportDeskPro.Contracts.Tickets;

public record AISuggestRequest(string Title, string Description);

public record AISuggestResponse(
    string SuggestedCategory,
    string SuggestedPriority,
    decimal Confidence,
    string Reasoning);