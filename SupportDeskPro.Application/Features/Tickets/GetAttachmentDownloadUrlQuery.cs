using MediatR;

namespace SupportDeskPro.Application.Features.Tickets.GetAttachmentDownloadUrl;

public record GetAttachmentDownloadUrlQuery(
    Guid TicketId,
    Guid AttachmentId)
    : IRequest<string>;