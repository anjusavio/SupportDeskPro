/// <summary>
/// Request model for posting a new comment on a ticket.
/// IsInternal = true means agent-only note — hidden from customers.
/// </summary>
namespace SupportDeskPro.Contracts.Comments;

public record CreateCommentRequest(
    string Body,
    bool IsInternal
);