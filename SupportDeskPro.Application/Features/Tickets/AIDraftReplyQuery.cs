using MediatR;
using SupportDeskPro.Contracts.Tickets;

namespace SupportDeskPro.Application.Features.Tickets.AIDraftReply;

public record AIDraftReplyQuery(Guid TicketId, bool IsInternal) : IRequest<AIDraftReplyResponse>;