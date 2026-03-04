/// <summary>
/// Query model for Admin dashboard — returns tenant-wide statistics.
/// Scoped to current tenant via Global Query Filter.
/// </summary>
using MediatR;
using SupportDeskPro.Contracts.Dashboard;

namespace SupportDeskPro.Application.Features.Dashboard.GetAdminDashboard;

public record GetAdminDashboardQuery : IRequest<AdminDashboardResponse>;