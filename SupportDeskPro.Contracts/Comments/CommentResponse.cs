/// <summary>
/// Response models for ticket comment list and detail views.
/// </summary>
namespace SupportDeskPro.Contracts.Comments;

public record CommentResponse(
    Guid Id,
    Guid TicketId,
    Guid AuthorId,
    string AuthorName,
    string AuthorRole,
    string Body,
    bool IsInternal,
    bool IsEdited,
    DateTime? EditedAt,
    decimal? SentimentScore,
    string? SentimentLabel,
    DateTime CreatedAt
);