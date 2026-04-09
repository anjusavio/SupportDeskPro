using MediatR;
using SupportDeskPro.Contracts.Tickets;

namespace SupportDeskPro.Application.Features.Tickets.AICategorizationSuggest;

public record AISuggestQuery(string Title,string Description) : IRequest<AISuggestResponse>;