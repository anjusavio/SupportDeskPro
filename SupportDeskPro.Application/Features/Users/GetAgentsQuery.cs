// Query model for fetching active agents list for assignment dropdown
using MediatR;
using SupportDeskPro.Contracts.Users;

namespace SupportDeskPro.Application.Features.Users.GetAgents;

public record GetAgentsQuery : IRequest<List<AgentSummaryResponse>>;