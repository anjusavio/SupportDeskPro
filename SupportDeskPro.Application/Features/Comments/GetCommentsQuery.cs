/// <summary>
/// Query model for fetching all comments on a ticket.
/// Customers see only public comments.
/// Agents and Admins see all comments including internal notes.
/// </summary>
using MediatR;
using SupportDeskPro.Contracts.Comments;

namespace SupportDeskPro.Application.Features.Comments.GetComments;

public record GetCommentsQuery(
    Guid TicketId,
    bool IsCustomer
) : IRequest<List<CommentResponse>>;