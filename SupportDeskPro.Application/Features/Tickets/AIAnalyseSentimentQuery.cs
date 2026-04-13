using MediatR;
using SupportDeskPro.Contracts.Tickets;

namespace SupportDeskPro.Application.Features.Tickets.AIAnalyseSentiment;

/// <summary>
/// Query to analyse customer sentiment for a given ticket.
/// Loads ticket description and all customer replies from DB.
/// Returns sentiment level, trigger phrases and agent advice.
/// </summary>
public record AIAnalyseSentimentQuery(Guid TicketId) : IRequest<AISentimentAnalysisResponse>;