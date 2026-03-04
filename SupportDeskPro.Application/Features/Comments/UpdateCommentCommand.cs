/// <summary>
/// Command model for editing a comment body.
/// RequesterId used to verify only original author can edit.
/// </summary>
using MediatR;

namespace SupportDeskPro.Application.Features.Comments.UpdateComment;

public record UpdateCommentCommand(
    Guid CommentId,
    Guid RequesterId,
    string Body
) : IRequest<UpdateCommentResult>;

public record UpdateCommentResult(bool Success,string Message);