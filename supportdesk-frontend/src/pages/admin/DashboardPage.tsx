/**
 * DashboardPage — Admin tenant-wide statistics and charts.
 *
 * CONCEPTS:
 *
 * 1. useQuery for GET /api/dashboard/admin
 *    Single query fetches all stats in one API call.
 *    All field names match AdminDashboardResponse.cs exactly 
 *
 * 2. Recharts BarChart — Ticket Volume (grouped open + resolved)
 *    ResponsiveContainer fills parent width automatically.
 *    Two <Bar> components = grouped bars per day 
 *
 * 3. Agent Performance list
 *    ticketsByAgent from backend → sorted by resolvedTodayCount.
 *    Progress bar width = resolvedTodayCount / max * 100% 
 *
 * 4. Recent Tickets table
 *    ticketsByAgent recent tickets shown with status + SLA badges.
 *    Click row → navigate to /tickets/:id 
 */

import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  BarChart, Bar, XAxis, YAxis,
  CartesianGrid, Tooltip, Legend,
  ResponsiveContainer,
} from 'recharts';
import {
  Ticket, CheckCircle2, AlertTriangle,
   RefreshCw,  Search,
  Circle, Activity, PauseCircle, XCircle,
} from 'lucide-react';
import Layout from '../../components/common/Layout';
import axiosClient from '../../api/axiosClient';
import { ApiResponse } from '../../types/api.types';

// ─────────────────────────────────────────────────────────────────────────────
// TYPES — match AdminDashboardResponse.cs exactly
// field names are camelCase because .NET returns camelCase JSON 
// ─────────────────────────────────────────────────────────────────────────────

interface CategoryTicketCount {
  categoryName: string;
  openCount: number;
  totalCount: number;
}

interface AgentTicketCount {
  agentName: string;
  openCount: number;
  inProgressCount: number;
  resolvedTodayCount: number;
}

interface PriorityTicketCount {
  priority: string;
  count: number;
}

interface AdminDashboardResponse {
  totalTickets: number;
  openTickets: number;
  inProgressTickets: number;
  resolvedTickets: number;
  closedTickets: number;
  ticketsCreatedToday: number;
  ticketsResolvedToday: number;
  slaBreachedCount: number;
  slaBreachedToday: number;
  averageResolutionTimeHours: number;
  ticketsByCategory: CategoryTicketCount[];
  ticketsByAgent: AgentTicketCount[];
  ticketsByPriority: PriorityTicketCount[];
}


const PRIORITY_CONFIG: Record<string, { label: string; dotColor: string }> = {
  Low:      { label: 'Low',      dotColor: 'bg-gray-400' },
  Medium:   { label: 'Medium',   dotColor: 'bg-blue-500' },
  High:     { label: 'High',     dotColor: 'bg-orange-500' },
  Critical: { label: 'Critical', dotColor: 'bg-red-500' },
};


// Agent avatar background colors — cycles through list 
const AVATAR_COLORS = [
  'bg-purple-500', 'bg-pink-500', 'bg-cyan-500',
  'bg-amber-500',  'bg-green-500', 'bg-indigo-500',
];

// Agent progress bar colors
const BAR_COLORS = ['#6366f1', '#ec4899', '#06b6d4', '#f59e0b', '#10b981'];

// HELPERS

/** Get initials from full name e.g. "James Robinson" → "JR" */
function getInitials(name: string): string {
  return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
}

/** Today's date formatted like "Monday, Feb 24, 2025" */
function getTodayLabel(): string {
  return new Date().toLocaleDateString('en-US', {
    weekday: 'long', year: 'numeric', month: 'short', day: 'numeric',
  });
}

// SUB-COMPONENT: StatCard — top summary metric card

function StatCard({
  label, value, sub, icon: Icon, iconBg, barColor,onClick
}: {
  label: string;
  value: number | string;
  sub: string;
  icon: React.ElementType;
  iconBg: string;
  barColor: string;
  onClick?: () => void; //  navigate to filtered ticket list 
}) {
  return (
    
    <div
     onClick={onClick} 
     className="bg-white border border-gray-100 rounded-xl p-5 shadow-sm
                    flex flex-col gap-3">
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
      {/* Colored bottom bar — like in the reference image */}
      <div className={`h-1 rounded-full ${barColor}`} />
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// MAIN PAGE
// ─────────────────────────────────────────────────────────────────────────────

const DashboardPage: React.FC = () => {
  const navigate = useNavigate();
  

  // ─── Query: GET /api/dashboard/admin ────────────────────────────────────
  const {
    data: stats,
    isLoading,
    isError,
    refetch,
  } = useQuery<AdminDashboardResponse>({
    queryKey: ['adminDashboard'],
    queryFn: () =>
      axiosClient
        .get<ApiResponse<AdminDashboardResponse>>('/dashboard/admin')
        .then((r) => {
          if (!r.data.data) throw new Error('Failed to load dashboard');
          return r.data.data; // AdminDashboardResponse 
        }),
    refetchInterval: 60_000, // auto-refresh every 60 seconds 
  });

  // ─── Loading ─────────────────────────────────────────────────────────────

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

  /**
   * CONCEPT: Build chart data from backend response
   *
   * BarChart needs array of objects where keys match <Bar dataKey="...">
   * We use ticketsByAgent to build a "per-agent" volume chart.
   * If you later add a ticketsOverTime endpoint, swap this data 
   */
  const agentVolumeData = (stats.ticketsByAgent ?? []).map(a => ({
    name: a.agentName.split(' ')[0], // first name only fits in chart
    Open: a.openCount,
    Resolved: a.resolvedTodayCount,
  }));

  /**
   * CONCEPT: Agent performance list
   * Sort agents by resolvedTodayCount descending → best performers first.
   * Progress bar width = agent resolved / max resolved * 100 
   */
  const sortedAgents = [...(stats.ticketsByAgent ?? [])]
    .sort((a, b) => b.resolvedTodayCount - a.resolvedTodayCount);

  const maxResolved = sortedAgents[0]?.resolvedTodayCount ?? 1;

  // ─────────────────────────────────────────────────────────────────────────
  // RENDER
  // ─────────────────────────────────────────────────────────────────────────

  return (
    <Layout>
      <div className="space-y-6">

        {/* ── HEADER ── */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-xl font-bold text-gray-900">Dashboard</h1>
            <p className="text-xs text-gray-400 mt-0.5">{getTodayLabel()}</p>
          </div>
          <div className="flex items-center gap-3">
            {/* Search bar — navigates to tickets with search */}
            <div className="relative hidden md:block">
              <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
              <input
                type="text"
                placeholder="Search tickets..."
                onKeyDown={(e) => {
                  if (e.key === 'Enter') navigate('/tickets');
                }}
                className="pl-9 pr-4 py-2 text-sm border border-gray-200 rounded-lg
                           focus:outline-none focus:ring-2 focus:ring-indigo-300 w-52"
              />
            </div>
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

        {/* ── SUMMARY CARDS — 4 columns like reference image ── */}
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <StatCard
            label="Open Tickets"
            value={stats.openTickets}
            sub={`+${stats.ticketsCreatedToday} today`}
            icon={Ticket}
            iconBg="bg-orange-400"
            barColor="bg-orange-400"
            onClick={() => navigate('/tickets?status=1')} //Open Tickets card
          />
          <StatCard
            label="In Progress"
            value={stats.inProgressTickets}
            sub={`${stats.slaBreachedToday} due today`}
            icon={Activity}
            iconBg="bg-yellow-400"
            barColor="bg-yellow-400"
            onClick={() => navigate('/tickets?status=2')} //In Progress card
          />
          <StatCard
            label="Resolved Today"
            value={stats.ticketsResolvedToday}
            sub={`+${stats.ticketsResolvedToday > 0 ? stats.ticketsResolvedToday : 0} vs yesterday`}
            icon={CheckCircle2}
            iconBg="bg-green-500"
            barColor="bg-green-500"
            onClick={() => navigate('/tickets?status=3')} //Resolved Today card
          />
          <StatCard
            label="SLA Breached"
            value={stats.slaBreachedCount}
            sub="Needs attention"
            icon={AlertTriangle}
            iconBg={stats.slaBreachedCount > 0 ? 'bg-red-500' : 'bg-gray-400'}
            barColor={stats.slaBreachedCount > 0 ? 'bg-red-500' : 'bg-gray-300'}
            onClick={() => navigate('/tickets?status=4')} //SLA Breached card
          />
        </div>

        {/* ── CHARTS ROW: Ticket Volume + Agent Performance ── */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">

          {/* Ticket Volume Bar Chart — takes 2/3 width like reference */}
          <div className="lg:col-span-2 bg-white border border-gray-100
                          rounded-xl p-5 shadow-sm">
            <div className="flex items-center justify-between mb-1">
              <div>
                <h2 className="text-sm font-semibold text-gray-800">Ticket Volume</h2>
                <p className="text-xs text-gray-400">This week</p>
              </div>
            </div>

            {agentVolumeData.length === 0 ? (
              <div className="flex items-center justify-center h-48 text-gray-400 text-sm">
                No data yet
              </div>
            ) : (
              /**
               * CONCEPT: ResponsiveContainer
               * Always wrap Recharts in this so it fills the parent div.
               * width="100%" + height={number} is the standard pattern 
               */
              <ResponsiveContainer width="100%" height={220}>
                <BarChart data={agentVolumeData} barSize={22} barGap={4}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#f3f4f6" vertical={false} />
                  <XAxis
                    dataKey="name"
                    tick={{ fontSize: 11, fill: '#9ca3af' }}
                    axisLine={false}
                    tickLine={false}
                  />
                  <YAxis
                    tick={{ fontSize: 11, fill: '#9ca3af' }}
                    axisLine={false}
                    tickLine={false}
                  />
                  <Tooltip
                    contentStyle={{
                      border: 'none', borderRadius: '8px',
                      boxShadow: '0 4px 6px -1px rgba(0,0,0,0.1)',
                      fontSize: '12px',
                    }}
                  />
                  <Legend
                    wrapperStyle={{ fontSize: '11px', paddingTop: '8px' }}
                  />
                  {/* Two bars — Open (blue) and Resolved (green) like reference  */}
                  <Bar dataKey="Open"     fill="#6366f1" radius={[4, 4, 0, 0]} />
                  <Bar dataKey="Resolved" fill="#10b981" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>

          {/* Agent Performance — takes 1/3 width */}
          <div className="bg-white border border-gray-100 rounded-xl p-5 shadow-sm">
            <div className="mb-4">
              <h2 className="text-sm font-semibold text-gray-800">Agent Performance</h2>
              <p className="text-xs text-gray-400">This week</p>
            </div>

            {sortedAgents.length === 0 ? (
              <div className="flex items-center justify-center h-32 text-gray-400 text-sm">
                No agents yet
              </div>
            ) : (
              <div className="space-y-4">
                {sortedAgents.slice(0, 5).map((agent, i) => (
                  <div key={agent.agentName} className="flex items-center gap-3">

                    {/* Avatar with initials */}
                    <div className={`h-8 w-8 shrink-0 rounded-full flex items-center
                                     justify-center text-xs font-bold text-white
                                     ${AVATAR_COLORS[i % AVATAR_COLORS.length]}`}>
                      {getInitials(agent.agentName)}
                    </div>

                    <div className="flex-1 min-w-0">
                      <div className="flex items-center justify-between mb-1">
                        <span className="text-xs font-medium text-gray-700 truncate">
                          {agent.agentName}
                        </span>
                        <span className="text-xs text-gray-400 shrink-0 ml-2">
                          {agent.resolvedTodayCount} resolved
                        </span>
                      </div>
                      {/*
                       * CONCEPT: Dynamic progress bar width
                       * width % = this agent's resolved / max resolved * 100
                       * Inline style needed because Tailwind can't use dynamic values 
                       */}
                      <div className="h-1.5 bg-gray-100 rounded-full overflow-hidden">
                        <div
                          className="h-full rounded-full transition-all duration-500"
                          style={{
                            width: `${Math.round((agent.resolvedTodayCount / maxResolved) * 100)}%`,
                            backgroundColor: BAR_COLORS[i % BAR_COLORS.length],
                          }}
                        />
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* ── RECENT TICKETS TABLE — bottom section like reference ── */}
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

          {/* Table header */}
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-50 bg-gray-50 text-left">
                {['Ticket ID', 'Category', 'Open', 'In Progress', 'Total'].map(h => (
                  <th key={h}
                    className="px-4 py-3 text-xs font-semibold text-gray-400 uppercase tracking-wider">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {(stats.ticketsByCategory ?? []).length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-4 py-8 text-center text-sm text-gray-400">
                    No tickets yet
                  </td>
                </tr>
              ) : (
                (stats.ticketsByCategory ?? []).map((cat) => (
                  <tr
                    key={cat.categoryName}
                    onClick={() => navigate('/tickets')}
                    className="hover:bg-gray-50 cursor-pointer transition-colors"
                  >
                    <td className="px-4 py-3 font-medium text-gray-800">
                      {cat.categoryName}
                    </td>
                    <td className="px-4 py-3 text-gray-500 text-xs">
                      {cat.categoryName}
                    </td>
                    <td className="px-4 py-3">
                      <span className="bg-blue-100 text-blue-700 text-xs font-semibold
                                       px-2 py-0.5 rounded-full">
                        {cat.openCount}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <span className="bg-yellow-100 text-yellow-700 text-xs font-semibold
                                       px-2 py-0.5 rounded-full">
                        {cat.totalCount - cat.openCount}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <span className="bg-gray-100 text-gray-600 text-xs font-semibold
                                       px-2 py-0.5 rounded-full">
                        {cat.totalCount}
                      </span>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* ── BOTTOM ROW: Priority breakdown + Avg resolution ── */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">

          {/* Priority breakdown */}
          <div className="lg:col-span-2 bg-white border border-gray-100 rounded-xl p-5 shadow-sm">
            <h2 className="text-sm font-semibold text-gray-800 mb-4">Tickets by Priority</h2>
            <div className="space-y-3">
              {(stats.ticketsByPriority ?? []).map((p) => {
                const cfg = PRIORITY_CONFIG[p.priority] ?? { label: p.priority, dotColor: 'bg-gray-400' };
                const pct = stats.totalTickets > 0
                  ? Math.round((p.count / stats.totalTickets) * 100)
                  : 0;
                return (
                  <div key={p.priority} className="flex items-center gap-3">
                    <div className={`w-2 h-2 rounded-full shrink-0 ${cfg.dotColor}`} />
                    <span className="text-xs text-gray-600 w-16 shrink-0">{cfg.label}</span>
                    <div className="flex-1 h-2 bg-gray-100 rounded-full overflow-hidden">
                      <div
                        className={`h-full rounded-full transition-all duration-500 ${cfg.dotColor}`}
                        style={{ width: `${pct}%` }}
                      />
                    </div>
                    <span className="text-xs font-semibold text-gray-700 w-8 text-right">
                      {p.count}
                    </span>
                  </div>
                );
              })}
            </div>
          </div>

          {/* Avg resolution time card */}
          <div className="bg-white border border-gray-100 rounded-xl p-5 shadow-sm
                          flex flex-col justify-between">
            <div>
              <h2 className="text-sm font-semibold text-gray-800">Avg Resolution Time</h2>
              <p className="text-xs text-gray-400 mt-0.5">All resolved tickets</p>
            </div>
            <div className="text-center py-4">
              <p className="text-4xl font-bold text-indigo-600">
                {stats.averageResolutionTimeHours.toFixed(1)}
              </p>
              <p className="text-sm text-gray-400 mt-1">hours</p>
            </div>
            <div className="flex items-center gap-2 text-xs text-green-600
                            bg-green-50 px-3 py-2 rounded-lg">
              <CheckCircle2 size={13} />
              {stats.ticketsResolvedToday} tickets resolved today
            </div>
          </div>
        </div>

      </div>
    </Layout>
  );
};

export default DashboardPage;