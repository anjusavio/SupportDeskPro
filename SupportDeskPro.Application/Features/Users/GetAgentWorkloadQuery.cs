// Query model for Admin dashboard showing open ticket count per agent
using MediatR;
using SupportDeskPro.Contracts.Users;

namespace SupportDeskPro.Application.Features.Users.GetAgentWorkload;

public record GetAgentWorkloadQuery
    : IRequest<List<AgentWorkloadResponse>>;