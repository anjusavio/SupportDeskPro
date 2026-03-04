/// <summary>
/// Handles comment soft deletion.
/// Admin can delete any comment.
/// Non-admin users can only delete their own comments.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Enums;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Comments.DeleteComment;

public class DeleteCommentCommandHandler
    : IRequestHandler<DeleteCommentCommand, DeleteCommentResult>
{
    private readonly IApplicationDbContext _db;

    public DeleteCommentCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DeleteCommentResult> Handle(
        DeleteCommentCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Find comment
        var comment = await _db.TicketComments
            .FirstOrDefaultAsync(
                c => c.Id == request.CommentId && !c.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException("Comment", request.CommentId);

        // 2. Admin can delete any comment
        // Non-admin can only delete their own
        var isAdmin = request.RequesterRole == UserRole.Admin.ToString();

        if (!isAdmin && comment.AuthorId != request.RequesterId)
            throw new ForbiddenException("You can only delete your own comments.");

        // 3. Soft delete
        comment.IsDeleted = true;
        comment.DeletedAt = DateTime.UtcNow;
        comment.DeletedBy = request.RequesterId;

        await _db.SaveChangesAsync(cancellationToken);

        return new DeleteCommentResult(true, "Comment deleted successfully.");
    }
}