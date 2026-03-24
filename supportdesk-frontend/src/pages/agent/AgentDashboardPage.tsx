/**
 * AgentDashboardPage — personal performance stats for logged-in agent.
 *
 * CONCEPTS:
 *
 * 1. useQuery for GET /api/dashboard/agent
 *    AgentId extracted from JWT on backend automatically.
 *    Frontend just calls endpoint — no need to pass agentId 
 *
 * 2. Role-based redirect
 *    Agent logs in → redirects to /agent-dashboard
 *    Different from Admin /dashboard 
 *
 * 3. Recent tickets list
 *    Backend returns List<RecentTicketSummary>.
 *    Click row → navigate to /tickets/:id 
 *
 * 4. SLA pending tickets highlighted
 *    mySLAPendingCount shown with warning color.
 *    Encourages agent to act before breach 
 */

import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  Clock, CheckCircle2, AlertTriangle,
  Activity, TrendingUp, RefreshCw,
  Circle, PauseCircle, XCircle, Ticket,
} from 'lucide-react';
import Layout from '../../components/common/Layout';
import axiosClient from '../../api/axiosClient';
import { ApiResponse } from '../../types/api.types';

// ─────────────────────────────────────────────────────────────────────────────
// TYPES — match AgentDashboardResponse.cs exactly
// ─────────────────────────────────────────────────────────────────────────────

interface RecentTicketSummary {
  id: string;
  ticketNumber: number;
  title: string;
  status: string;
  priority: string;
  isSLABreached: boolean;
  createdAt: string;
}

interface AgentDashboardResponse {
  myTotalAssigned: number;
  myOpenTickets: number;
  myInProgressTickets: number;
  myResolvedToday: number;
  mySLABreachedCount: number;
  mySLAPendingCount: number;
  myAverageResolutionTimeHours: number;
  myRecentTickets: RecentTicketSummary[];
}

// ─────────────────────────────────────────────────────────────────────────────
// CONFIG MAPS
// ─────────────────────────────────────────────────────────────────────────────

const STATUS_CONFIG: Record<string, {
  label: string; color: string; icon: React.ElementType;
}> = {
  Open:       { label: 'Open',        color: 'bg-blue-100 text-blue-700',     icon: Circle },
  InProgress: { label: 'In Progress', color: 'bg-yellow-100 text-yellow-700', icon: Activity },
  OnHold:     { label: 'On Hold',     color: 'bg-orange-100 text-orange-700', icon: PauseCircle },
  Resolved:   { label: 'Resolved',    color: 'bg-green-100 text-green-700',   icon: CheckCircle2 },
  Closed:     { label: 'Closed',      color: 'bg-gray-100 text-gray-600',     icon: XCircle },
};

const PRIORITY_CONFIG: Record<string, { label: string; dotColor: string }> = {
  Low:      { label: 'Low',      dotColor: 'bg-gray-400' },
  Medium:   { label: 'Medium',   dotColor: 'bg-blue-500' },
  High:     { label: 'High',     dotColor: 'bg-orange-500' },
  Critical: { label: 'Critical', dotColor: 'bg-red-500' },
};

// ─────────────────────────────────────────────────────────────────────────────
// HELPERS
// ─────────────────────────────────────────────────────────────────────────────

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleString('en-US', {
    month: 'short', day: 'numeric',
    hour: '2-digit', minute: '2-digit',
  });
}

function getTodayLabel(): string {
  return new Date().toLocaleDateString('en-US', {
    weekday: 'long', year: 'numeric', month: 'short', day: 'numeric',
  });
}

// ─────────────────────────────────────────────────────────────────────────────
// SUB-COMPONENT: StatCard
// ─────────────────────────────────────────────────────────────────────────────

function StatCard({
  label, value, sub, icon: Icon, iconBg, barColor, onClick,
}: {
  label: string;
  value: number | string;
  sub: string;
  icon: React.ElementType;
  iconBg: string;
  barColor: string;
  onClick?: () => void;
}) {
  return (
    <div
      onClick={onClick}
      className={`bg-white border border-gray-100 rounded-xl p-5 shadow-sm
                  flex flex-col gap-3 ${onClick ? 'cursor-pointer hover:shadow-md transition-shadow' : ''}`}
    >
      <div className="flex items-start justify-between">
        <div>
          <p className="text-xs text-gray-500 font-medium">{label}</p>
          <p className="text-3xl font-bold text-gray-900 mt-1">{value}</p>
          <p className="text-xs text-gray-400 mt-1">{sub}</p>
        </div>
        <div className={`p-2 rounded-lg ${iconBg}`}>
          <Icon size={18} className="text-white" />
        </div>
      </div>
      <div className={`h-1 rounded-full ${barColor}`} />
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// SUB-COMPONENT: StatusBadge + PriorityBadge
// ─────────────────────────────────────────────────────────────────────────────

function StatusBadge({ status }: { status: string }) {
  const cfg = STATUS_CONFIG[status] ?? { label: status, color: 'bg-gray-100 text-gray-600', icon: Circle };
  const Icon = cfg.icon;
  return (
    <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold ${cfg.color}`}>
      <Icon size={11} />{cfg.label}
    </span>
  );
}

function PriorityBadge({ priority }: { priority: string }) {
  const cfg = PRIORITY_CONFIG[priority] ?? { label: priority, dotColor: 'bg-gray-400' };
  return (
    <span className="inline-flex items-center gap-1.5 text-xs text-gray-600">
      <span className={`w-2 h-2 rounded-full ${cfg.dotColor}`} />
      {cfg.label}
    </span>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// MAIN PAGE COMPONENT
// ─────────────────────────────────────────────────────────────────────────────

const AgentDashboardPage: React.FC = () => {
  const navigate = useNavigate();

  // ─── Query: GET /api/dashboard/agent ────────────────────────────
  const {
    data: stats,
    isLoading,
    isError,
    refetch,
  } = useQuery<AgentDashboardResponse>({
    queryKey: ['agentDashboard'],
    queryFn: () =>
      axiosClient
        .get<ApiResponse<AgentDashboardResponse>>('/dashboard/agent')
        .then((r) => {
          if (!r.data.data) throw new Error('Failed to load dashboard');
          return r.data.data;
        }),
    refetchInterval: 60_000, // auto-refresh every 60s 
  });

  // ─── Loading ─────────────────────────────────────────────────────
  if (isLoading) {
    return (
      <Layout>
        <div className="flex items-center justify-center h-96">
          <div className="flex flex-col items-center gap-3 text-gray-400">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600" />
            <span className="text-sm">Loading dashboard...</span>
          </div>
        </div>
      </Layout>
    );
  }

  if (isError || !stats) {
    return (
      <Layout>
        <div className="flex flex-col items-center justify-center h-96 gap-3">
          <p className="text-gray-500 text-sm">Failed to load dashboard.</p>
          <button onClick={() => refetch()}
            className="text-sm text-indigo-600 hover:underline">
            Try again
          </button>
        </div>
      </Layout>
    );
  }

  // ─────────────────────────────────────────────────────────────────
  // RENDER
  // ─────────────────────────────────────────────────────────────────

  return (
    <Layout>
      <div className="space-y-6">

        {/* ── HEADER ── */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-xl font-bold text-gray-900">My Dashboard</h1>
            <p className="text-xs text-gray-400 mt-0.5">{getTodayLabel()}</p>
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={() => navigate('/tickets')}
              className="flex items-center gap-2 px-3 py-2 text-sm font-medium
                         bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
            >
              <Ticket size={14} /> View All Tickets
            </button>
            <button
              onClick={() => refetch()}
              className="p-2 border border-gray-200 rounded-lg hover:bg-gray-50
                         text-gray-500 transition-colors"
              title="Refresh"
            >
              <RefreshCw size={15} />
            </button>
          </div>
        </div>

        {/* ── SUMMARY CARDS ── */}
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <StatCard
            label="Total Assigned"
            value={stats.myTotalAssigned}
            sub="all assigned tickets"
            icon={Ticket}
            iconBg="bg-indigo-500"
            barColor="bg-indigo-500"
            onClick={() => navigate('/tickets')}
          />
          <StatCard
            label="Open"
            value={stats.myOpenTickets}
            sub={`${stats.myInProgressTickets} in progress`}
            icon={Clock}
            iconBg="bg-blue-500"
            barColor="bg-blue-500"
            onClick={() => navigate('/tickets?status=1')}
          />
          <StatCard
            label="Resolved Today"
            value={stats.myResolvedToday}
            sub="great work! 🎉"
            icon={CheckCircle2}
            iconBg="bg-green-500"
            barColor="bg-green-500"
          />
          <StatCard
            label="SLA Breached"
            value={stats.mySLABreachedCount}
            sub={`${stats.mySLAPendingCount} approaching deadline`}
            icon={AlertTriangle}
            iconBg={stats.mySLABreachedCount > 0 ? 'bg-red-500' : 'bg-gray-400'}
            barColor={stats.mySLABreachedCount > 0 ? 'bg-red-500' : 'bg-gray-300'}
            onClick={() => navigate('/tickets?isSLABreached=true')}
          />
        </div>

        {/* ── SECOND ROW: Avg Resolution + SLA Pending ── */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">

          {/* Avg resolution time */}
          <div className="bg-white border border-gray-100 rounded-xl p-5 shadow-sm
                          flex flex-col justify-between">
            <div>
              <h2 className="text-sm font-semibold text-gray-800">
                Avg Resolution Time
              </h2>
              <p className="text-xs text-gray-400 mt-0.5">My resolved tickets</p>
            </div>
            <div className="text-center py-4">
              <p className="text-4xl font-bold text-indigo-600">
                {stats.myAverageResolutionTimeHours.toFixed(1)}
              </p>
              <p className="text-sm text-gray-400 mt-1">hours</p>
            </div>
            <div className="flex items-center gap-2 text-xs text-green-600
                            bg-green-50 px-3 py-2 rounded-lg">
              <TrendingUp size={13} />
              {stats.myResolvedToday} tickets resolved today
            </div>
          </div>

          {/* My ticket breakdown */}
          <div className="lg:col-span-2 bg-white border border-gray-100 rounded-xl p-5 shadow-sm">
            <h2 className="text-sm font-semibold text-gray-800 mb-4">
              My Ticket Breakdown
            </h2>
            <div className="space-y-3">
              {[
                { label: 'Open',        count: stats.myOpenTickets,        color: 'bg-blue-500',   max: stats.myTotalAssigned },
                { label: 'In Progress', count: stats.myInProgressTickets,  color: 'bg-yellow-400', max: stats.myTotalAssigned },
                { label: 'Resolved',    count: stats.myResolvedToday,      color: 'bg-green-500',  max: stats.myTotalAssigned },
                { label: 'SLA Pending', count: stats.mySLAPendingCount,    color: 'bg-orange-400', max: stats.myTotalAssigned },
                { label: 'SLA Breached',count: stats.mySLABreachedCount,   color: 'bg-red-500',    max: stats.myTotalAssigned },
              ].map((item) => {
                const pct = item.max > 0 ? Math.round((item.count / item.max) * 100) : 0;
                return (
                  <div key={item.label} className="flex items-center gap-3">
                    <span className="text-xs text-gray-600 w-24 shrink-0">{item.label}</span>
                    <div className="flex-1 h-2 bg-gray-100 rounded-full overflow-hidden">
                      <div
                        className={`h-full rounded-full transition-all duration-500 ${item.color}`}
                        style={{ width: `${pct}%` }}
                      />
                    </div>
                    <span className="text-xs font-semibold text-gray-700 w-6 text-right">
                      {item.count}
                    </span>
                  </div>
                );
              })}
            </div>
          </div>
        </div>

        {/* ── RECENT TICKETS TABLE ── */}
        <div className="bg-white border border-gray-100 rounded-xl shadow-sm overflow-hidden">
          <div className="flex items-center justify-between px-5 py-4 border-b border-gray-100">
            <h2 className="text-sm font-semibold text-gray-800">Recent Tickets</h2>
            <button
              onClick={() => navigate('/tickets')}
              className="text-xs text-indigo-600 hover:underline font-medium"
            >
              View all →
            </button>
          </div>

          {(stats.myRecentTickets ?? []).length === 0 ? (
            <div className="flex items-center justify-center h-32 text-gray-400 text-sm">
              No tickets assigned yet
            </div>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-50 bg-gray-50 text-left">
                  {['Ticket', 'Status', 'Priority', 'SLA', 'Created'].map(h => (
                    <th key={h} className="px-4 py-3 text-xs font-semibold text-gray-400 uppercase tracking-wider">
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {(stats.myRecentTickets ?? []).map((ticket) => (
                  <tr
                    key={ticket.id}
                    onClick={() => navigate(`/tickets/${ticket.id}`)}
                    className="hover:bg-gray-50 cursor-pointer transition-colors"
                  >
                    {/* Ticket number + title */}
                    <td className="px-4 py-3">
                      <div className="flex flex-col gap-0.5">
                        <span className="text-[11px] font-mono text-indigo-500">
                          #{ticket.ticketNumber}
                        </span>
                        <span className="font-medium text-gray-800 truncate max-w-[240px]">
                          {ticket.title}
                        </span>
                      </div>
                    </td>

                    <td className="px-4 py-3">
                      <StatusBadge status={ticket.status} />
                    </td>

                    <td className="px-4 py-3">
                      <PriorityBadge priority={ticket.priority} />
                    </td>

                    {/* SLA breach indicator */}
                    <td className="px-4 py-3">
                      {ticket.isSLABreached ? (
                        <span className="flex items-center gap-1 text-xs font-semibold
                                         text-red-700 bg-red-100 px-2 py-0.5 rounded-full w-fit">
                          <AlertTriangle size={11} /> Breached
                        </span>
                      ) : (
                        <span className="text-xs text-green-600 flex items-center gap-1">
                          <CheckCircle2 size={11} /> On track
                        </span>
                      )}
                    </td>

                    <td className="px-4 py-3 text-xs text-gray-500">
                      {formatDate(ticket.createdAt)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

      </div>
    </Layout>
  );
};

export default AgentDashboardPage;