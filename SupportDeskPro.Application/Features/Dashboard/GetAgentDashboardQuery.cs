/// <summary>
/// Query model for Agent dashboard — returns personal ticket
/// statistics for the currently authenticated agent.
/// </summary>
using MediatR;
using SupportDeskPro.Contracts.Dashboard;

namespace SupportDeskPro.Application.Features.Dashboard.GetAgentDashboard;

public record GetAgentDashboardQuery(Guid AgentId) : IRequest<AgentDashboardResponse>;