/// <summary>
/// Command model for soft deleting a comment.
/// Admin can delete any comment.
/// Author can delete their own comment.
/// </summary>
using MediatR;

namespace SupportDeskPro.Application.Features.Comments.DeleteComment;

public record DeleteCommentCommand(
    Guid CommentId,
    Guid RequesterId,
    string RequesterRole
) : IRequest<DeleteCommentResult>;

public record DeleteCommentResult(bool Success,string Message);