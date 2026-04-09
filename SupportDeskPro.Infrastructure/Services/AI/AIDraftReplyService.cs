using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SupportDeskPro.Application.Interfaces;

namespace SupportDeskPro.Infrastructure.Services;

/// <summary>
/// Uses Claude AI to draft a professional support reply
/// based on the ticket content and conversation history.
/// Agent always reviews and edits before sending — AI never sends automatically.
/// </summary>
public class AIDraftReplyService : IAIDraftReplyService
{
    private readonly AnthropicClient _client;
    private readonly ILogger<AIDraftReplyService> _logger;

    public AIDraftReplyService(
        IConfiguration configuration,
        ILogger<AIDraftReplyService> logger)
    {
        var apiKey = configuration["AnthropicSettings:ApiKey"]
            ?? throw new InvalidOperationException("Anthropic API key not configured.");
        _client = new AnthropicClient(apiKey);
        _logger = logger;
    }

    public async Task<string> DraftReplyAsync(
        string ticketTitle,
        string ticketDescription,
        string customerName,
        List<string> conversationHistory,
        bool isInternal,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Build conversation context
            var history = conversationHistory.Any()
                ? string.Join("\n", conversationHistory.Select((m, i) => $"{i + 1}. {m}"))
                : "No previous messages.";

            //Prompt will veriy depends on Internal(agent to admin and vice versa) or not(admin/agent to customer) 
            var prompt = isInternal
               ? $"""
                    You are a support agent writing an internal note for your team.
                    This note is NOT visible to the customer.

                    Ticket Title: {ticketTitle}
                    Ticket Description: {ticketDescription}

                    Previous conversation:
                    {string.Join("\n", conversationHistory)}

                    Guidelines:
                    - No greeting needed — this is an internal note
                    - Be direct and technical
                    - Include investigation findings, next steps, or escalation notes
                    - Keep it under 100 words
                    - Write as a brief professional internal note

                    Write ONLY the note text. No metadata.
                    """
               : $"""
                    You are a professional customer support agent replying to a customer.
                    This reply WILL be visible to the customer.

                    Customer Name: {customerName}
                    Ticket Title: {ticketTitle}
                    Ticket Description: {ticketDescription}

                    Previous conversation:
                    {string.Join("\n", conversationHistory)}

                    Guidelines:
                    - Start with a greeting using the customer's first name
                    - Acknowledge their issue briefly
                    - Provide clear actionable steps
                    - Be professional but friendly and empathetic
                    - End with an offer to help further
                    - Keep it under 150 words
                    - Do NOT use placeholders like [your name]

                    Write ONLY the reply text. No metadata.
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

            var draft = response.Content
                .OfType<TextContent>()
                .FirstOrDefault()?.Text ?? string.Empty;

            return draft.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AI draft reply failed for ticket: {Title}", ticketTitle);
            return string.Empty; // return empty — agent types manually 
        }
    }
}