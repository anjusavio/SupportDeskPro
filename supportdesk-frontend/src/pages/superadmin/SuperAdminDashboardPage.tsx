/**
 * SuperAdminDashboardPage — platform-wide overview for SuperAdmin.
 *
 * CONCEPTS:
 *
 * 1. No tenant scoping
 *    Every other dashboard is scoped to one tenant.
 *    This page shows data across ALL tenants simultaneously.
 *    SuperAdmin is the only role that sees this view.
 *
 * 2. SLA health classification
 *    Each tenant gets a health badge based on SLA compliance rate:
 *    Good    = 90%+ — green  — performing well
 *    AtRisk  = 70-89% — amber — needs attention
 *    Poor    = below 70% — red — requires intervention
 *
 * 3. Parallel data loading
 *    Backend runs all aggregation queries in parallel using Task.WhenAll.
 *    Single API call returns everything needed for the full page.
 *
 * 4. Cached response
 *    Backend caches result for 5 minutes — cross-tenant aggregation
 *    is expensive and does not need to be real-time.
 *
 * 5. Tenant activity table
 *    Sorted by ticket volume — most active tenants at the top.
 *    Clicking a row navigates to tenant detail on TenantsPage.
 */

import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  Building2, Users, Ticket, CheckCircle2,
  Mail, AlertTriangle, TrendingUp, Clock
} from 'lucide-react';
import Layout from '../../components/common/Layout';
import axiosClient from '../../api/axiosClient';
import { ApiResponse } from '../../types/api.types';

// ── Types ─────────────────────────────────────────────────────────
interface TenantActivity {
  tenantId: string;
  tenantName: string;
  slug: string;
  totalTickets: number;
  openTickets: number;
  resolvedTickets: number;
  totalAgents: number;
  totalCustomers: number;
  slaComplianceRate: number;
  slaHealth: string; // Good | AtRisk | Poor
  isActive: boolean;
  createdAt: string;
}

interface RecentTenant {
  tenantId: string;
  tenantName: string;
  slug: string;
  planType: number;
  createdAt: string;
}

interface SuperAdminDashboard {
  totalTenants: number;
  activeTenants: number;
  inactiveTenants: number;
  totalUsers: number;
  totalAgents: number;
  totalTickets: number;
  ticketsToday: number;
  totalEmailsSent: number;
  emailFailures: number;
  platformSLAComplianceRate: number;
  tenantActivity: TenantActivity[];
  recentTenants: RecentTenant[];
}

// ── SLA health config ─────────────────────────────────────────────
const SLA_HEALTH_CONFIG = {
  Good:   { color: 'text-green-600', bg: 'bg-green-100', dot: '🟢' },
  AtRisk: { color: 'text-amber-600', bg: 'bg-amber-100', dot: '🟡' },
  Poor:   { color: 'text-red-600',   bg: 'bg-red-100',   dot: '🔴' },
};

// ── Helper ────────────────────────────────────────────────────────
function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', {
    month: 'short', day: 'numeric', year: 'numeric'
  });
}

function timeAgo(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const days = Math.floor(diff / 86400000);
  if (days === 0) return 'Today';
  if (days === 1) return 'Yesterday';
  return `${days} days ago`;
}

// ── Stat Card ─────────────────────────────────────────────────────
function StatCard({
  icon: Icon, label, value, sub, color
}: {
  icon: React.ElementType;
  label: string;
  value: string | number;
  sub?: string;
  color: string;
}) {
  return (
    <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm">
      <div className="flex items-start justify-between">
        <div>
          <p className="text-xs font-medium text-gray-400 uppercase tracking-wider mb-1">
            {label}
          </p>
          <p className="text-2xl font-bold text-gray-900">
            {typeof value === 'number' ? value.toLocaleString() : value}
          </p>
          {sub && (
            <p className="text-xs text-gray-400 mt-1">{sub}</p>
          )}
        </div>
        <div className={`p-2.5 rounded-xl ${color}`}>
          <Icon size={18} />
        </div>
      </div>
    </div>
  );
}

// ── Main Component ────────────────────────────────────────────────
const SuperAdminDashboardPage: React.FC = () => {
  const navigate = useNavigate();

  const { data: dashboard, isLoading } = useQuery<SuperAdminDashboard>({
    queryKey: ['superadmin-dashboard'],
    queryFn: () =>
      axiosClient
        .get<ApiResponse<SuperAdminDashboard>>('/dashboard/superadmin')
        .then(r => r.data.data!),
    refetchInterval: 5 * 60 * 1000, // refetch every 5 minutes
  });

  if (isLoading) {
    return (
      <Layout>
        <div className="flex items-center justify-center h-96">
          <div className="flex flex-col items-center gap-3 text-gray-500">
            <div className="animate-spin rounded-full h-8 w-8
                            border-b-2 border-indigo-600" />
            <span className="text-sm">Loading platform data...</span>
          </div>
        </div>
      </Layout>
    );
  }

  if (!dashboard) return null;

  return (
    <Layout>
      <div className="max-w-7xl mx-auto space-y-6">

        {/* Header */}
        <div>
          <h1 className="text-2xl font-bold text-gray-900">
            Platform Overview
          </h1>
          <p className="text-sm text-gray-500 mt-1">
            Real-time health across all {dashboard.totalTenants} tenant workspaces
          </p>
        </div>

        {/* ── Overview Cards ── */}
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <StatCard
            icon={Building2}
            label="Total Tenants"
            value={dashboard.totalTenants}
            sub={`${dashboard.activeTenants} active · ${dashboard.inactiveTenants} inactive`}
            color="bg-indigo-50 text-indigo-600"
          />
          <StatCard
            icon={Users}
            label="Total Users"
            value={dashboard.totalUsers}
            sub={`${dashboard.totalAgents} agents across platform`}
            color="bg-blue-50 text-blue-600"
          />
          <StatCard
            icon={Ticket}
            label="Total Tickets"
            value={dashboard.totalTickets}
            sub={`${dashboard.ticketsToday} created today`}
            color="bg-emerald-50 text-emerald-600"
          />
          <StatCard
            icon={CheckCircle2}
            label="SLA Compliance"
            value={`${dashboard.platformSLAComplianceRate}%`}
            sub="across all tenants"
            color={
              dashboard.platformSLAComplianceRate >= 90
                ? 'bg-green-50 text-green-600'
                : dashboard.platformSLAComplianceRate >= 70
                  ? 'bg-amber-50 text-amber-600'
                  : 'bg-red-50 text-red-600'
            }
          />
        </div>

        {/* ── Email Stats ── */}
        <div className="grid grid-cols-2 gap-4">
          <StatCard
            icon={Mail}
            label="Emails Sent"
            value={dashboard.totalEmailsSent}
            color="bg-purple-50 text-purple-600"
          />
          <StatCard
            icon={AlertTriangle}
            label="Email Failures"
            value={dashboard.emailFailures}
            sub={dashboard.emailFailures > 0
              ? 'Check email service configuration'
              : 'All emails delivered successfully'}
            color={dashboard.emailFailures > 0
              ? 'bg-red-50 text-red-600'
              : 'bg-green-50 text-green-600'}
          />
        </div>

        {/* ── Tenant Activity Table ── */}
        <div className="bg-white border border-gray-200 rounded-xl
                        shadow-sm overflow-hidden">
          <div className="flex items-center justify-between px-6 py-4
                          border-b border-gray-100">
            <div className="flex items-center gap-2">
              <TrendingUp size={16} className="text-indigo-600" />
              <h2 className="text-sm font-semibold text-gray-700">
                Tenant Activity
              </h2>
              <span className="text-xs text-gray-400">
                — sorted by ticket volume
              </span>
            </div>
            <button
              onClick={() => navigate('/superadmin/tenants')}
              className="text-xs text-indigo-600 hover:underline"
            >
              View all tenants →
            </button>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-gray-50 border-b border-gray-100">
                <tr>
                  {[
                    'Tenant', 'Tickets', 'Open',
                    'Resolved', 'Agents', 'Customers',
                    'SLA Compliance', 'Status'
                  ].map(h => (
                    <th key={h}
                      className="px-5 py-3 text-left text-xs font-semibold
                                 text-gray-500 uppercase whitespace-nowrap">
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {dashboard.tenantActivity.map((tenant, index) => {
                  const health = SLA_HEALTH_CONFIG[
                    tenant.slaHealth as keyof typeof SLA_HEALTH_CONFIG
                  ] ?? SLA_HEALTH_CONFIG.Good;

                  return (
                    <tr
                      key={tenant.tenantId}
                      onClick={() => navigate('/superadmin/tenants')}
                      className="hover:bg-gray-50 transition-colors cursor-pointer"
                    >
                      <td className="px-5 py-4">
                        <div className="flex items-center gap-3">
                          <span className="text-xs text-gray-400 w-4">
                            {index + 1}
                          </span>
                          <div>
                            <p className="text-sm font-medium text-gray-900">
                              {tenant.tenantName}
                            </p>
                            <p className="text-xs font-mono text-gray-400">
                              {tenant.slug}
                            </p>
                          </div>
                        </div>
                      </td>
                      <td className="px-5 py-4">
                        <span className="text-sm font-semibold text-gray-900">
                          {tenant.totalTickets.toLocaleString()}
                        </span>
                      </td>
                      <td className="px-5 py-4">
                        <span className="text-sm text-amber-600 font-medium">
                          {tenant.openTickets}
                        </span>
                      </td>
                      <td className="px-5 py-4">
                        <span className="text-sm text-green-600 font-medium">
                          {tenant.resolvedTickets}
                        </span>
                      </td>
                      <td className="px-5 py-4">
                        <span className="text-sm text-gray-600">
                          {tenant.totalAgents}
                        </span>
                      </td>
                      <td className="px-5 py-4">
                        <span className="text-sm text-gray-600">
                          {tenant.totalCustomers}
                        </span>
                      </td>
                      <td className="px-5 py-4">
                        <div className="flex items-center gap-2">
                          <span className={`text-xs font-bold px-2 py-0.5
                                          rounded-full ${health.bg} ${health.color}`}>
                            {health.dot} {tenant.slaComplianceRate}%
                          </span>
                        </div>
                      </td>
                      <td className="px-5 py-4">
                        {tenant.isActive ? (
                          <span className="text-xs font-medium text-green-600">
                            Active
                          </span>
                        ) : (
                          <span className="text-xs font-medium text-red-500">
                            Inactive
                          </span>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>

        {/* ── Recent Tenant Registrations ── */}
        {dashboard.recentTenants.length > 0 && (
          <div className="bg-white border border-gray-200 rounded-xl
                          p-5 shadow-sm">
            <div className="flex items-center gap-2 mb-4">
              <Clock size={16} className="text-indigo-600" />
              <h2 className="text-sm font-semibold text-gray-700">
                Recent Tenant Registrations
              </h2>
            </div>
            <div className="space-y-3">
              {dashboard.recentTenants.map(tenant => (
                <div key={tenant.tenantId}
                  className="flex items-center justify-between
                             py-2 border-b border-gray-50 last:border-0">
                  <div className="flex items-center gap-3">
                    <div className="h-8 w-8 rounded-lg bg-indigo-100
                                    flex items-center justify-center">
                      <Building2 size={14} className="text-indigo-600" />
                    </div>
                    <div>
                      <p className="text-sm font-medium text-gray-900">
                        {tenant.tenantName}
                      </p>
                      <p className="text-xs font-mono text-gray-400">
                        {tenant.slug}
                      </p>
                    </div>
                  </div>
                  <div className="text-right">
                    <p className="text-xs font-medium text-gray-500">
                      {timeAgo(tenant.createdAt)}
                    </p>
                    <p className="text-[10px] text-gray-400">
                      {formatDate(tenant.createdAt)}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </Layout>
  );
};

export default SuperAdminDashboardPage;