/// <summary>
/// Request model for editing an existing comment body.
/// Only the original author can edit their comment.
/// </summary>
namespace SupportDeskPro.Contracts.Comments;

public record UpdateCommentRequest(string Body);