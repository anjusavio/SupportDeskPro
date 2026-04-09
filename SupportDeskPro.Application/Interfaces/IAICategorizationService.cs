namespace SupportDeskPro.Application.Interfaces;

public interface IAICategorizationService
{
    Task<AICategorizationResult> SuggestCategoryAndPriorityAsync(
        string title,
        string description,
        List<string> availableCategories,
        CancellationToken cancellationToken = default);
}

public record AICategorizationResult(
    string SuggestedCategory,
    string SuggestedPriority,
    decimal Confidence,
    string Reasoning
);