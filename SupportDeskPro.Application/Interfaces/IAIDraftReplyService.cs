namespace SupportDeskPro.Application.Interfaces;

public interface IAIDraftReplyService
{
    Task<string> DraftReplyAsync(
        string ticketTitle,
        string ticketDescription,
        string customerName,
        List<string> conversationHistory,
        bool isInternal,
        CancellationToken cancellationToken = default);
}