/// <summary>
/// Handles AI-powered ticket categorization suggestions.
/// Called from the frontend while the customer is typing their ticket.
/// Fetches active categories from the database and passes them to the
/// AI service along with the ticket title and description.
/// Claude AI analyses the content and returns a suggested category,
/// priority, confidence score, and a brief reasoning explanation.
/// AI failure returns a safe default — never blocks the customer 
/// </summary>


using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Tickets;

namespace SupportDeskPro.Application.Features.Tickets.AICategorizationSuggest;

public class AISuggestQueryHandler : IRequestHandler<AISuggestQuery, AISuggestResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IAICategorizationService _aiService;

    public AISuggestQueryHandler(
        IApplicationDbContext db,
        IAICategorizationService aiService)
    {
        _db = db;
        _aiService = aiService;
    }

    public async Task<AISuggestResponse> Handle(AISuggestQuery request,CancellationToken cancellationToken)
    {
        // Get active categories for AI context
        var categories = await _db.Categories
            .Where(c => c.IsActive)
            .Select(c => c.Name)
            .ToListAsync(cancellationToken);

        // Call AI service
        var result = await _aiService.SuggestCategoryAndPriorityAsync(
            request.Title,
            request.Description,
            categories,
            cancellationToken);

        return new AISuggestResponse(
            result.SuggestedCategory,
            result.SuggestedPriority,
            result.Confidence,
            result.Reasoning);
    }
}