/// <summary>
/// Handles comment editing — enforces author-only edit rule.
/// Marks comment as edited and records edit timestamp.
/// </summary>
using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Comments.UpdateComment;

public class UpdateCommentCommandHandler
    : IRequestHandler<UpdateCommentCommand, UpdateCommentResult>
{
    private readonly IApplicationDbContext _db;

    public UpdateCommentCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UpdateCommentResult> Handle(
        UpdateCommentCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Find comment
        var comment = await _db.TicketComments
            .FirstOrDefaultAsync(
                c => c.Id == request.CommentId && !c.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException("Comment", request.CommentId);

        // 2. Only original author can edit
        if (comment.AuthorId != request.RequesterId)
            throw new ForbiddenException("You can only edit your own comments.");

        // 3. Update comment
        comment.Body = request.Body.Trim();
        comment.IsEdited = true;
        comment.EditedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new UpdateCommentResult(true, "Comment updated successfully.");
    }
}