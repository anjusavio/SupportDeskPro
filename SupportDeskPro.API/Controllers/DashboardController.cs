/// <summary>
/// REST controller for dashboard statistics.
/// Admin dashboard returns tenant-wide ticket and SLA metrics.
/// Agent dashboard returns personal performance statistics.
/// </summary>
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportDeskPro.Application.Features.Dashboard.GetAdminDashboard;
using SupportDeskPro.Application.Features.Dashboard.GetAgentDashboard;
using SupportDeskPro.Contracts.Common;
using SupportDeskPro.Contracts.Dashboard;
using SupportDeskPro.Domain.Entities;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Claims;

namespace SupportDeskPro.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns tenant-wide dashboard statistics for Admin.
    /// Includes ticket counts by status, SLA breach metrics,
    /// agent workload breakdown, category distribution
    /// and average resolution time.
    /// All data scoped to current tenant automatically.
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminDashboard()
    {
        var result = await _mediator.Send(new GetAdminDashboardQuery());
        return Ok(ApiResponse<AdminDashboardResponse>.Ok(result));
    }

    /// <summary>
    /// Returns personal performance dashboard for the authenticated agent.
    /// Includes assigned ticket counts, SLA breach stats,
    /// tickets approaching SLA deadline and average resolution time.
    /// AgentId extracted from JWT claims automatically.
    /// </summary>
    [HttpGet("agent")]
    [Authorize(Roles = "Agent")]
    public async Task<IActionResult> GetAgentDashboard()
    {
        var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _mediator.Send(new GetAgentDashboardQuery(agentId));
        return Ok(ApiResponse<AgentDashboardResponse>.Ok(result));
    }
}
