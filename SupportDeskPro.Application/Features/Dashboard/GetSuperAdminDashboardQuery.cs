using MediatR;
using SupportDeskPro.Contracts.Dashboard;

namespace SupportDeskPro.Application.Features.Dashboard.GetSuperAdminDashboard;

/// <summary>
/// Query to fetch platform-wide dashboard statistics for SuperAdmin.
/// No tenant scoping — SuperAdmin sees all data across all tenants.
/// Results cached for 5 minutes to avoid hammering the database.
/// </summary>
public record GetSuperAdminDashboardQuery : IRequest<SuperAdminDashboardResponse>;