using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SupportDeskPro.Application.Common;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Tickets;
using SupportDeskPro.Domain.Enums;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Tickets.AIGetSimilarTickets;

/// <summary>
/// Two-stage approach to find similar resolved tickets:
///
/// Stage 1 — SQL pre-filter (fast, free):
///   Loads the current ticket, then fetches up to 20 resolved tickets
///   from the same category first. If fewer than 10 found, fills
///   remaining slots from other categories. This narrows candidates
///   before sending to Claude — controlling cost and latency.
///
/// Stage 2 — Claude semantic scoring (smart):
///   Sends candidates to Claude Haiku which scores each for semantic
///   similarity. Claude understands meaning not just keywords —
///   "Cannot login" and "locked out of account" score as similar.
///   Returns top 3 above 0.5 threshold ordered by score.
///
/// AI failure returns empty list — no errors shown to agent.
/// </summary>
public class AIGetSimilarTicketsQueryHandler
    : IRequestHandler<AIGetSimilarTicketsQuery, List<AISimilarTicketResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IAISimilarTicketService _aiSimilarService;
    private readonly IMemoryCache _cache;
    public AIGetSimilarTicketsQueryHandler(
        IApplicationDbContext db,
        IAISimilarTicketService aiSimilarService,IMemoryCache cache)
    {
        _db = db;
        _aiSimilarService = aiSimilarService;
        _cache = cache;
    }

    public async Task<List<AISimilarTicketResponse>> Handle(
        AIGetSimilarTicketsQuery request,
        CancellationToken cancellationToken)
    {

        var cacheKey = CacheKeys.SimilarTickets(request.TicketId);

        // Return cached result — saves Claude API call 
        if (_cache.TryGetValue(cacheKey,out List<AISimilarTicketResponse>? cached))
            return cached!;


        // Load current ticket 
        var ticket = await _db.Tickets
            .FirstOrDefaultAsync(
                t => t.Id == request.TicketId && !t.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        // ── Stage 1: SQL pre-filter ───────────────────────────────────

        // fetching from same category to get the relevant data
        var sameCategoryTickets = await _db.Tickets
            .Include(t => t.Category)
            .Include(t => t.Comments
                .Where(c => !c.IsInternal && !c.IsDeleted))
            .ThenInclude(c => c.Author)
            .Where(t => t.Id != request.TicketId
                        && !t.IsDeleted
                        && t.CategoryId == ticket.CategoryId
                        && (t.Status == TicketStatus.Resolved
                            || t.Status == TicketStatus.Closed))
            .OrderByDescending(t => t.ResolvedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        var candidates = sameCategoryTickets;

        
        if (!candidates.Any())
            return new List<AISimilarTicketResponse>();

        // ── Stage 2: Claude semantic scoring
        // -Claude reads the ticket and gives a similarity score based on MEANING not just matching words 

        var candidateList = candidates.Select(t => new CandidateTicket(
            t.Id,
            t.TicketNumber,
            t.Title,
            t.Description,
            t.Comments
                .Where(c => !c.IsInternal && c.Author != null)
                .OrderByDescending(c => c.CreatedAt )
                  .Select(c => {
                      var authorName = c.Author?.FirstName ?? "Agent";
                      var authorRole = c.Author?.Role.ToString() ?? "Support";
                      return $"{authorName} ({authorRole}): {c.Body}";
                  })
           .ToList()
        )).ToList();

        // Claude scores similarity
        var aiResults = await _aiSimilarService.FindSimilarTicketsAsync(
            ticket.Title,
            ticket.Description,
            candidateList,
            cancellationToken);

        if (!aiResults.Any())
            return new List<AISimilarTicketResponse>();

        // ── Stage 3: Extract resolution from conversation ─────────────

        var result = new List<AISimilarTicketResponse>();

        foreach (var ai in aiResults)
        {
            var t = candidates.First(c => c.Id == ai.Id);

            // Build conversation list
            var conversation = t.Comments
                .Where(c => !c.IsInternal && c.Author != null)
                .OrderBy(c => c.CreatedAt)
                .Select(c => {
                    var authorName = c.Author?.FirstName ?? "Agent";
                    var authorRole = c.Author?.Role.ToString() ?? "Support";
                    return $"{authorName} ({authorRole}): {c.Body}";
                })
                .ToList();

            //  Claude reads full conversation and extracts the resolution
            var resolution = await _aiSimilarService.ExtractResolutionAsync(
                t.Title,
                conversation,
                cancellationToken);

            result.Add(new AISimilarTicketResponse(
                t.Id,
                t.TicketNumber,
                t.Title,
                t.Category.Name,
                t.Status.ToString(),
                resolution,         //  AI extracted resolution 
                ai.SimilarityScore,
                t.ResolvedAt ?? DateTime.UtcNow));
        }

        // Cache result for 1 hour
        _cache.Set(cacheKey, result, TimeSpan.FromHours(1));

        return result;
    }
}