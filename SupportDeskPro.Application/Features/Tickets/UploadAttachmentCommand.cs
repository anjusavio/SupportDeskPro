using MediatR;
using SupportDeskPro.Contracts.Tickets;

namespace SupportDeskPro.Application.Features.Tickets.UploadAttachment;

public record UploadAttachmentCommand(
    Guid TicketId,
    Guid? CommentId,        // null = ticket-level, set = comment-level
    Guid UploadedById,
    Guid TenantId,
    Stream FileStream,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes)
    : IRequest<AttachmentResponse>;