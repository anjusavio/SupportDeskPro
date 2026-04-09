using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SupportDeskPro.Application.Interfaces;
using System.Text.Json;

namespace SupportDeskPro.Infrastructure.Services;

/// <summary>
/// Uses Claude AI to suggest ticket category and priority
/// based on the ticket title and description.
/// Suggestions are stored separately — never override customer selection.
/// </summary>
public class AICategorizationService : IAICategorizationService
{
    private readonly AnthropicClient _client;
    private readonly ILogger<AICategorizationService> _logger;

    public AICategorizationService(
        IConfiguration configuration,
        ILogger<AICategorizationService> logger)
    {
        var apiKey = configuration["AnthropicSettings:ApiKey"]
            ?? throw new InvalidOperationException("Anthropic API key not configured.");

        _client = new AnthropicClient(apiKey);
        _logger = logger;
    }

    public async Task<AICategorizationResult> SuggestCategoryAndPriorityAsync(
        string title,
        string description,
        List<string> availableCategories,
        CancellationToken cancellationToken = default)
    {

        try
        {
            _logger.LogInformation("Calling Anthropic API...");

            var categoriesList = string.Join(", ", availableCategories);

           
            var jsonFormat = """
            {
                "suggestedCategory": "exact category name from the list",
                "suggestedPriority": "Low|Medium|High|Critical",
                "confidence": 0.95,
                "reasoning": "brief one sentence explanation"
            }
            """;

            var prompt = $"""
                You are a support ticket classification assistant.
                Analyze the following support ticket and suggest the most appropriate 
                category and priority.

                Available categories: {categoriesList}

                Priority levels:
                - Low: General questions, non-urgent requests
                - Medium: Issues affecting work but have a workaround
                - High: Significant impact, needs attention today
                - Critical: Completely blocked, cannot work at all

                Ticket Title: {title}
                Ticket Description: {description}

                Respond ONLY with a JSON object in this exact format, nothing else:
                {jsonFormat}
                """;

            var response = await _client.Messages.GetClaudeMessageAsync(new MessageParameters
              {

                  Model = "claude-haiku-4-5-20251001",
                  MaxTokens = 200,
                  Messages = new List<Message>
                  {
                    new Message
                    {
                        Role = RoleType.User,
                        Content = new List<ContentBase>
                        {
                            new TextContent
                            {
                                Text = prompt
                            }
                        }
                    }
                  }
              },
              cancellationToken);
            _logger.LogInformation("Anthropic response received");

            // Get response text
            var responseText = response.Content
                .OfType<TextContent>()
                .FirstOrDefault()?.Text ?? string.Empty;

            _logger.LogInformation("Response text: {Text}", responseText);
            
            if (string.IsNullOrWhiteSpace(responseText))
            {
                _logger.LogWarning("Empty response from Anthropic"); 
                return DefaultResult();
            }

            //The AI is returning the response wrapped in markdown code blocks instead of pure JSON.
            //strip markdown code blocks before parsing
            responseText = responseText
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();


            // Parse JSON response
            var result = JsonSerializer.Deserialize<AIResponseDto>(responseText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            _logger.LogInformation("Parsed result: {Category} {Priority}",
                        result?.SuggestedCategory, result?.SuggestedPriority);
            
            if (result is null)
                return DefaultResult();

            return new AICategorizationResult(
                SuggestedCategory: result.SuggestedCategory,
                SuggestedPriority: result.SuggestedPriority,
                Confidence: result.Confidence,
                Reasoning: result.Reasoning
            );
        }
        catch (Exception ex)
        {
            // AI failure must never fail ticket creation 
            _logger.LogError(ex,"AI categorization failed. Type: {Type} Message: {Message}",
               ex.GetType().Name,
               ex.Message);

            return DefaultResult();
        }
    }

    // Safe fallback when AI fails or is unavailable
    private static AICategorizationResult DefaultResult() =>
        new("General Inquiry", "Medium", 0, "AI suggestion unavailable");

    private class AIResponseDto
    {
        public string SuggestedCategory { get; set; } = string.Empty;
        public string SuggestedPriority { get; set; } = string.Empty;
        public decimal Confidence { get; set; }
        public string Reasoning { get; set; } = string.Empty;
    }
}