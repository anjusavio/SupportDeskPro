using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Tickets;
using SupportDeskPro.Domain.Enums;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Tickets.AIAnalyseSentiment;

/// <summary>
/// Handles sentiment analysis for a support ticket.
///
/// Loads the ticket description and all public customer replies
/// to give Claude the full picture of the customer's emotional journey.
/// A customer who starts neutral but becomes frustrated after repeated
/// replies should be detected as Frustrated — not just the first message.
///
/// Only customer messages are analysed — agent replies are excluded
/// since we want to measure the customer's tone, not the agent's.
///
/// AI failure returns Neutral — agent workflow is never blocked.
/// </summary>
public class AIAnalyseSentimentQueryHandler
    : IRequestHandler<AIAnalyseSentimentQuery, AISentimentAnalysisResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IAISentimentService _sentimentService;

    public AIAnalyseSentimentQueryHandler(
        IApplicationDbContext db,
        IAISentimentService sentimentService)
    {
        _db = db;
        _sentimentService = sentimentService;
    }

    public async Task<AISentimentAnalysisResponse> Handle(
        AIAnalyseSentimentQuery request,
        CancellationToken cancellationToken)
    {
        // Load ticket with customer comments only
        var ticket = await _db.Tickets
            .Include(t => t.Comments
                .Where(c => !c.IsInternal && !c.IsDeleted))
            .ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(
                t => t.Id == request.TicketId && !t.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        // Extract only customer messages — agent replies excluded
        // We want to measure customer tone, not agent tone 
        var customerMessages = ticket.Comments
            .Where(c => c.Author != null
                        && c.Author.Role == UserRole.Customer)
            .OrderBy(c => c.CreatedAt)
            .Select(c => c.Body)
            .ToList();

        // Analyse sentiment from description + customer replies
        var result = await _sentimentService.AnalyseSentimentAsync(
            ticket.Description,
            customerMessages,
            cancellationToken);

        return new AISentimentAnalysisResponse(
            result.Level,
            result.Label,
            result.Confidence,
            result.TriggerPhrases,
            result.AgentAdvice);
    }
}