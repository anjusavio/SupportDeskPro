using System.Text.Json;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SupportDeskPro.Application.Interfaces;

namespace SupportDeskPro.Infrastructure.Services;

/// <summary>
/// Uses Claude Haiku to detect customer sentiment from ticket content.
///
/// Analyses the original ticket description plus any subsequent
/// customer replies to build a complete picture of their emotional state.
///
/// Three sentiment levels:
///   Frustrated — urgent/angry language, repeated attempts, strong frustration
///   Concerned  — confused, uncertain, mildly worried tone
///   Neutral    — polite, factual, calm description
///
/// Returns trigger phrases so agents can see exactly what flagged the sentiment.
/// Returns agent advice so agents know how to respond appropriately.
/// AI failure returns Neutral — never blocks the agent workflow.
/// </summary>
public class AISentimentService : IAISentimentService
{
    private readonly AnthropicClient _client;
    private readonly ILogger<AISentimentService> _logger;

    public AISentimentService(
        IConfiguration configuration,
        ILogger<AISentimentService> logger)
    {
        var apiKey = configuration["AnthropicSettings:ApiKey"]
            ?? throw new InvalidOperationException(
                "Anthropic API key not configured.");
        _client = new AnthropicClient(apiKey);
        _logger = logger;
    }

    public async Task<SentimentResult> AnalyseSentimentAsync(
        string ticketDescription,
        List<string> customerMessages,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sentiment analysis starting");

            // Combine description and customer replies for full context
            var allContent = new List<string> { ticketDescription };
            allContent.AddRange(customerMessages);
            var fullContent = string.Join("\n\n", allContent);

            var jsonFormat = """
                {
                    "level": "Frustrated",
                    "label": "Frustrated — High Priority Tone",
                    "confidence": 0.92,
                    "triggerPhrases": ["still broken", "third time contacting"],
                    "agentAdvice": "Acknowledge frustration immediately. Escalate if not resolved within 1 hour."
                }
                """;

            var prompt = $"""
                You are a customer sentiment analyser for a support system.

                Analyse the emotional tone of the following customer support content
                and classify it into exactly one of three levels.

                Customer content to analyse:
                {fullContent}

                Sentiment levels:
                - Frustrated: Customer uses urgent or angry language, mentions 
                  repeated attempts, says "still broken", "unacceptable", 
                  "third time", "ridiculous", or expresses strong dissatisfaction
                - Concerned: Customer seems confused or uncertain, uses phrases 
                  like "not sure why", "I think", "I'm confused", mildly worried
                  but not angry
                - Neutral: Customer describes issue politely and factually,
                  no emotional language, calm and professional tone

                Agent advice guide:
                - Frustrated: Acknowledge frustration immediately, prioritise, escalate if needed
                - Concerned: Be extra clear and patient, explain steps carefully
                - Neutral: Standard professional response is fine

                Return ONLY a raw JSON object matching this format exactly.
                No markdown, no explanation, no code blocks.
                triggerPhrases must be exact short quotes from the text above.
                If no trigger phrases exist return empty array.
                Format: {jsonFormat}
                """;

            var response = await _client.Messages.GetClaudeMessageAsync(
                new MessageParameters
                {
                    Model = "claude-haiku-4-5-20251001",
                    MaxTokens = 300,
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
                "Sentiment response: {Response}", responseText);

            var result = JsonSerializer.Deserialize<SentimentResultDto>(
                responseText,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (result is null)
                return NeutralResult();

            return new SentimentResult(
                result.Level,
                result.Label,
                result.Confidence,
                result.TriggerPhrases ?? Array.Empty<string>(),
                result.AgentAdvice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sentiment analysis failed");
            return NeutralResult(); // safe default — never blocks agent 
        }
    }

    // Safe fallback when AI fails — assume neutral, never alarm agent incorrectly
    private static SentimentResult NeutralResult() =>
        new(
            Level: "Neutral",
            Label: "Neutral — Normal Tone",
            Confidence: 0,
            TriggerPhrases: Array.Empty<string>(),
            AgentAdvice: "Standard professional response.");

    // Private inner class — only used here to deserialize Claude response
    private class SentimentResultDto
    {
        public string Level { get; set; } = "Neutral";
        public string Label { get; set; } = "Neutral — Normal Tone";
        public decimal Confidence { get; set; }
        public string[]? TriggerPhrases { get; set; }
        public string AgentAdvice { get; set; } = string.Empty;
    }
}