using System.Text.Json;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SupportDeskPro.Application.Interfaces;

namespace SupportDeskPro.Infrastructure.Services;

/// <summary>
/// Uses Claude Haiku to semantically score resolved tickets
/// for similarity against the current ticket.
/// Unlike keyword search, Claude understands meaning:
/// "Cannot login" and "locked out of account" are treated as similar.
/// SQL pre-filters candidates — Claude scores and ranks them.
/// AI failure returns empty list — no errors shown to agent.
/// </summary>
public class AISimilarTicketService : IAISimilarTicketService
{
    private readonly AnthropicClient _client;
    private readonly ILogger<AISimilarTicketService> _logger;

    public AISimilarTicketService(
        IConfiguration configuration,
        ILogger<AISimilarTicketService> logger)
    {
        var apiKey = configuration["AnthropicSettings:ApiKey"]
            ?? throw new InvalidOperationException(
                "Anthropic API key not configured.");
        _client = new AnthropicClient(apiKey);
        _logger = logger;
    }

    public async Task<List<SimilarTicketAIResult>> FindSimilarTicketsAsync(
        string currentTitle,
        string currentDescription,
        List<CandidateTicket> candidates,
        CancellationToken cancellationToken = default)
    {
        if (!candidates.Any())
            return new List<SimilarTicketAIResult>();

        try
        {
            _logger.LogInformation(
                "AI similar ticket search starting. Candidates: {Count}",
                candidates.Count);

            // Build candidate list for Claude prompt
            var candidateList = string.Join("\n\n",
                candidates.Select((c, i) =>
                    $"Ticket {i + 1}:\n" +
                    $"ID: {c.Id}\n" +
                    $"Title: {c.Title}\n" +
                    $"Description: {c.Description}\n" +
                    $"Conversation:\n" +
                    (c.Conversation.Any()
                        ? string.Join("\n", c.Conversation.Take(5)) // first 5 messages for context
                        : "No conversation yet")));

            var jsonFormat = """
                [
                    {
                        "id": "exact-guid-from-above",
                        "similarityScore": 0.95,
                        "reason": "one sentence explaining why similar"
                    }
                ]
                """;

            var prompt = $"""
                You are a support ticket similarity analyser.

                Current ticket that needs help:
                Title: {currentTitle}
                Description: {currentDescription}

                Compare the current ticket against these resolved tickets
                and identify the top 3 most similar ones:

                {candidateList}

                Scoring guide:
                - 0.9 to 1.0: Almost identical issue
                - 0.7 to 0.9: Very similar, same root cause likely
                - 0.5 to 0.7: Somewhat related, worth checking
                - Below 0.5: Not relevant, exclude completely

                Important rules:
                - Focus on semantic meaning, not just matching words
                - "Cannot login" and "locked out of account" are similar
                - "Payment failed" and "charge not processed" are similar
                - Only include tickets with score 0.5 or above
                - Return fewer than 3 if not enough relevant ones exist
                - Use the EXACT ticket ID (the GUID) from the list above

                Return ONLY a raw JSON array. No markdown, no explanation.
                Format: {jsonFormat}
                """;

            var response = await _client.Messages.GetClaudeMessageAsync(
                new MessageParameters
                {
                    Model = "claude-haiku-4-5-20251001",
                    MaxTokens = 500,
                    Messages = new List<Message>
                    {
                        new Message
                        {
                            Role = RoleType.User,
                            Content = new List<ContentBase>
                            {
                                new TextContent { Text = prompt }
                            }
                        }
                    }
                },
                cancellationToken);

            var responseText = response.Content
                .OfType<TextContent>()
                .FirstOrDefault()?.Text ?? string.Empty;

            // Strip markdown if Claude wraps anyway
            responseText = responseText
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            _logger.LogInformation(
                "AI similar ticket response: {Response}", responseText);

            var results = JsonSerializer.Deserialize<List<AIResultDto>>(
                responseText,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<AIResultDto>();

            return results
                .Where(r => r.SimilarityScore >= 0.5)
                .Select(r => new SimilarTicketAIResult(
                    Guid.Parse(r.Id),
                    r.SimilarityScore,
                    r.Reason))
                .OrderByDescending(r => r.SimilarityScore)
                .Take(3)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AI similar ticket search failed for: {Title}", currentTitle);
            return new List<SimilarTicketAIResult>();
        }
    }

    // Private inner class — only used here to deserialize Claude response
    private class AIResultDto
    {
        public string Id { get; set; } = string.Empty;
        public double SimilarityScore { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Reads the full ticket conversation and extracts the key resolution.
    /// otherwise will get reply like  "thank you" and "closing ticket".
    /// Returns a concise summary of what actually fixed the issue.
    /// </summary>
    public async Task<string> ExtractResolutionAsync(
        string ticketTitle,
        List<string> conversation,
        CancellationToken cancellationToken = default)
    {
        if (!conversation.Any())
            return string.Empty;

        try
        {
            var conversationText = string.Join("\n", conversation);

            //var prompt = $"""
            //You are a support knowledge base assistant.

            //Read this support ticket conversation and extract the key resolution.

            //Ticket: {ticketTitle}

            //Conversation:
            //{conversationText}

            //Rules:
            //- Identify what actually solved the issue
            //- Ignore pleasantries like "thank you", "glad to help", "closing ticket"
            //- Ignore questions asking for more information
            //- Write 1-3 concise sentences describing the fix
            //- Write as an actionable resolution agents can follow
            //- If no clear resolution exists, return empty string

            //Return ONLY the resolution text. No labels, no markdown.
            //""";


            var prompt = $"""
                You are a support knowledge base assistant.

                Read this resolved support ticket conversation and write 
                actionable steps an agent can share with a customer 
                who has a similar issue.

                Ticket: {ticketTitle}

                Conversation:
                {conversationText}

                Rules:
                - Write as direct instructions to the customer using "you"
                - write clear actionable points
                - Maximum 4 points
                - Keep each point short and simple
                - Ignore pleasantries like "thank you", "glad to help", "closing ticket"
                - Ignore questions asking for more information
                - start with "Hello" and End with an offer to help further
                - If no clear resolution exists return empty string

                Example format:
                - Clear your browser cache and try logging in again.
                - If the issue persists, reset your password via the Forgot Password link.
                - Try a different browser if the problem continues.

                Return ONLY the bullet points. No labels, no markdown, no explanation.
                """;

            var response = await _client.Messages.GetClaudeMessageAsync(
                new MessageParameters
                {
                    Model = "claude-haiku-4-5-20251001",
                    MaxTokens = 150,
                    Messages = new List<Message>
                    {
                    new Message
                    {
                        Role = RoleType.User,
                        Content = new List<ContentBase>
                        {
                            new TextContent { Text = prompt }
                        }
                    }
                    }
                },
                cancellationToken);

            var resolution = response.Content
                .OfType<TextContent>()
                .FirstOrDefault()?.Text ?? string.Empty;

            return resolution.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AI resolution extraction failed for: {Title}", ticketTitle);
            return string.Empty;
        }
    }
}