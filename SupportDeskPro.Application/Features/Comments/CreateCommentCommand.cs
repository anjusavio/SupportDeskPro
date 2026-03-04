/// <summary>
/// Command model for posting a new comment on a ticket.
/// Tracks first agent response for SLA first response time calculation.
/// </summary>
using MediatR;

namespace SupportDeskPro.Application.Features.Comments.CreateComment;

public record CreateCommentCommand(
    Guid TicketId,
    Guid AuthorId,
    string AuthorRole,
    string Body,
    bool IsInternal
) : IRequest<CreateCommentResult>;

public record CreateCommentResult(
    bool Success,
    string Message,
    Guid? CommentId = null
);