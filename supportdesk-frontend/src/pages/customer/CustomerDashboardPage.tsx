/**
 * CustomerDashboardPage — personal ticket overview for logged-in customer.
 *
 * CONCEPTS:
 *
 * 1. useQuery for GET /api/tickets/my
 *    Reuses existing endpoint — no new backend needed.
 *    Fetches all customer tickets to calculate summary stats 
 *
 * 2. Derived stats from ticket list
 *    No separate dashboard API needed.
 *    We calculate open/resolved/breached counts from the list 
 *
 * 3. Quick action button
 *    "Raise New Ticket" → navigate to /create-ticket 
 *
 * 4. Recent tickets list
 *    Shows last 5 tickets with status and SLA badge.
 *    Click row → navigate to /tickets/:id 
 */

import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  Ticket, CheckCircle2, AlertTriangle,
  Clock, Plus, RefreshCw, Circle,
  Activity, PauseCircle, XCircle,
} from 'lucide-react';
import Layout from '../../components/common/Layout';
import axiosClient from '../../api/axiosClient';
import useAuthStore from '../../store/authStore';
import { ApiResponse } from '../../types/api.types';

// ─────────────────────────────────────────────────────────────────────────────
// TYPES — match TicketResponse from backend
// ─────────────────────────────────────────────────────────────────────────────

interface TicketResponse {
  id: string;
  ticketNumber: string;
  title: string;
  status: string;
  priority: string;
  categoryName: string;
  createdAt: string;
  isSLABreached: boolean;
  slaResolutionDueAt: string | null;
}

interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
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

const PRIORITY_CONFIG: Record<string, { label: string; color: string }> = {
  Low:      { label: 'Low',      color: 'bg-gray-100 text-gray-600' },
  Medium:   { label: 'Medium',   color: 'bg-blue-100 text-blue-700' },
  High:     { label: 'High',     color: 'bg-orange-100 text-orange-700' },
  Critical: { label: 'Critical', color: 'bg-red-100 text-red-700' },
};

// ─────────────────────────────────────────────────────────────────────────────
// HELPERS
// ─────────────────────────────────────────────────────────────────────────────

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleString('en-US', {
    month: 'short', day: 'numeric', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  });
}

function getGreeting(): string {
  const hour = new Date().getHours();
  if (hour < 12) return 'Good morning';
  if (hour < 17) return 'Good afternoon';
  return 'Good evening';
}

// ─────────────────────────────────────────────────────────────────────────────
// SUB-COMPONENTS
// ─────────────────────────────────────────────────────────────────────────────

function StatCard({
  label, value, sub, icon: Icon, iconBg, barColor, onClick,
}: {
  label: string;
  value: number;
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

function StatusBadge({ status }: { status: string }) {
  const cfg = STATUS_CONFIG[status] ?? {
    label: status, color: 'bg-gray-100 text-gray-600', icon: Circle,
  };
  const Icon = cfg.icon;
  return (
    <span className={`inline-flex items-center gap-1 px-2 py-0.5
                      rounded-full text-xs font-semibold ${cfg.color}`}>
      <Icon size={11} />{cfg.label}
    </span>
  );
}

function PriorityBadge({ priority }: { priority: string }) {
  const cfg = PRIORITY_CONFIG[priority] ?? {
    label: priority, color: 'bg-gray-100 text-gray-600',
  };
  return (
    <span className={`inline-flex items-center px-2 py-0.5
                      rounded-full text-xs font-semibold ${cfg.color}`}>
      {cfg.label}
    </span>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// MAIN PAGE COMPONENT
// ─────────────────────────────────────────────────────────────────────────────

const CustomerDashboardPage: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuthStore();

  // ─── Query: GET /api/tickets/my ─────────────────────────────────
  /**
   * CONCEPT: Reuse existing endpoint
   * No new backend needed — we fetch all tickets
   * and calculate summary stats on the frontend 
   */
  const {
    data: ticketsData,
    isLoading,
    refetch,
  } = useQuery<PagedResult<TicketResponse>>({
    queryKey: ['myTickets', 'dashboard'],
    queryFn: () =>
      axiosClient
        .get<ApiResponse<PagedResult<TicketResponse>>>(
          '/tickets/my?page=1&pageSize=100'
        )
        .then((r) => {
          if (!r.data.data) throw new Error('Failed to load tickets');
          return r.data.data;
        }),
    refetchInterval: 60_000,
  });

  const allTickets = ticketsData?.items ?? [];

  /**
   * CONCEPT: Derived stats from ticket list
   * Calculate counts from the fetched array.
   * No extra API call needed 
   */
  const openTickets     = allTickets.filter(t => t.status === 'Open').length;
  const inProgressTickets = allTickets.filter(t => t.status === 'InProgress').length;
  const resolvedTickets = allTickets.filter(t =>
    t.status === 'Resolved' || t.status === 'Closed'
  ).length;
  const slaBreached     = allTickets.filter(t => t.isSLABreached).length;
  const totalTickets    = ticketsData?.totalCount ?? 0;

  // Recent 5 tickets for the table
  const recentTickets = allTickets.slice(0, 5);

  // ─────────────────────────────────────────────────────────────────
  // RENDER
  // ─────────────────────────────────────────────────────────────────

  return (
    <Layout>
      <div className="space-y-6">

        {/* ── HEADER ── */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-xl font-bold text-gray-900">
              {getGreeting()}, {user?.firstName}! 👋
            </h1>
            <p className="text-xs text-gray-400 mt-0.5">
              Here's an overview of your support tickets
            </p>
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={() => refetch()}
              className="p-2 border border-gray-200 rounded-lg hover:bg-gray-50
                         text-gray-500 transition-colors"
              title="Refresh"
            >
              <RefreshCw size={15} />
            </button>
            {/* Quick action — raise new ticket */}
            <button
              onClick={() => navigate('/create-ticket')}
              className="flex items-center gap-2 px-4 py-2 text-sm font-medium
                         bg-indigo-600 text-white rounded-lg hover:bg-indigo-700
                         transition-colors"
            >
              <Plus size={15} /> New Ticket
            </button>
          </div>
        </div>

        {/* ── SUMMARY CARDS ── */}
        {isLoading ? (
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            {[...Array(4)].map((_, i) => (
              <div key={i} className="bg-white border border-gray-100 rounded-xl
                                      p-5 h-28 animate-pulse bg-gray-50" />
            ))}
          </div>
        ) : (
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            <StatCard
              label="Total Tickets"
              value={totalTickets}
              sub="all time"
              icon={Ticket}
              iconBg="bg-indigo-500"
              barColor="bg-indigo-500"
              onClick={() => navigate('/my-tickets')}
            />
            <StatCard
              label="Open"
              value={openTickets + inProgressTickets}
              sub={`${inProgressTickets} in progress`}
              icon={Clock}
              iconBg="bg-blue-500"
              barColor="bg-blue-500"
              onClick={() => navigate('/my-tickets?status=2')}
            />
            <StatCard
              label="Resolved"
              value={resolvedTickets}
              sub="closed + resolved"
              icon={CheckCircle2}
              iconBg="bg-green-500"
              barColor="bg-green-500"
              onClick={() => navigate('/my-tickets?status=4')} // 4 = Resolved
            />
            <StatCard
              label="SLA Breached"
              value={slaBreached}
              sub={slaBreached > 0 ? 'needs attention' : 'all on track'}
              icon={AlertTriangle}
              iconBg={slaBreached > 0 ? 'bg-red-500' : 'bg-gray-400'}
              barColor={slaBreached > 0 ? 'bg-red-500' : 'bg-gray-300'}
            />
          </div>
        )}

        {/* ── QUICK ACTIONS ── */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">

          {/* Raise new ticket */}
          <button
            onClick={() => navigate('/create-ticket')}
            className="flex items-center gap-4 bg-white border border-gray-200
                       rounded-xl p-4 shadow-sm hover:shadow-md hover:border-indigo-300
                       transition-all text-left group"
          >
            <div className="h-10 w-10 rounded-xl bg-indigo-100 flex items-center
                            justify-center group-hover:bg-indigo-600 transition-colors">
              <Plus size={20} className="text-indigo-600 group-hover:text-white transition-colors" />
            </div>
            <div>
              <p className="font-semibold text-gray-800 text-sm">Raise New Ticket</p>
              <p className="text-xs text-gray-400 mt-0.5">Report an issue or request</p>
            </div>
          </button>

          {/* View all tickets */}
          <button
            onClick={() => navigate('/my-tickets')}
            className="flex items-center gap-4 bg-white border border-gray-200
                       rounded-xl p-4 shadow-sm hover:shadow-md hover:border-blue-300
                       transition-all text-left group"
          >
            <div className="h-10 w-10 rounded-xl bg-blue-100 flex items-center
                            justify-center group-hover:bg-blue-600 transition-colors">
              <Ticket size={20} className="text-blue-600 group-hover:text-white transition-colors" />
            </div>
            <div>
              <p className="font-semibold text-gray-800 text-sm">View All Tickets</p>
              <p className="text-xs text-gray-400 mt-0.5">Track your requests</p>
            </div>
          </button>

          {/* Open tickets quick link */}
          <button
            onClick={() => navigate('/my-tickets')}
            className="flex items-center gap-4 bg-white border border-gray-200
                       rounded-xl p-4 shadow-sm hover:shadow-md hover:border-green-300
                       transition-all text-left group"
          >
            <div className="h-10 w-10 rounded-xl bg-green-100 flex items-center
                            justify-center group-hover:bg-green-600 transition-colors">
              <CheckCircle2 size={20} className="text-green-600 group-hover:text-white transition-colors" />
            </div>
            <div>
              <p className="font-semibold text-gray-800 text-sm">
                {openTickets + inProgressTickets} Open Tickets
              </p>
              <p className="text-xs text-gray-400 mt-0.5">Awaiting resolution</p>
            </div>
          </button>
        </div>

        {/* ── RECENT TICKETS TABLE ── */}
        <div className="bg-white border border-gray-100 rounded-xl shadow-sm overflow-hidden">
          <div className="flex items-center justify-between px-5 py-4 border-b border-gray-100">
            <h2 className="text-sm font-semibold text-gray-800">Recent Tickets</h2>
            <button
              onClick={() => navigate('/my-tickets')}
              className="text-xs text-indigo-600 hover:underline font-medium"
            >
              View all →
            </button>
          </div>

          {isLoading ? (
            <div className="flex items-center justify-center h-32">
              <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-indigo-600" />
            </div>
          ) : recentTickets.length === 0 ? (
            <div className="flex flex-col items-center justify-center h-32 gap-2 text-gray-400">
              <Ticket size={28} className="opacity-30" />
              <p className="text-sm">No tickets yet</p>
              <button
                onClick={() => navigate('/create-ticket')}
                className="text-xs text-indigo-600 hover:underline"
              >
                Raise your first ticket
              </button>
            </div>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-50 bg-gray-50 text-left">
                  {['Ticket', 'Status', 'Priority', 'Category', 'SLA', 'Created'].map(h => (
                    <th key={h} className="px-4 py-3 text-xs font-semibold
                                           text-gray-400 uppercase tracking-wider">
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {recentTickets.map((ticket) => (
                  <tr
                    key={ticket.id}
                    onClick={() => navigate(`/tickets/${ticket.id}`)}
                    className="hover:bg-gray-50 cursor-pointer transition-colors"
                  >
                    <td className="px-4 py-3">
                      <div className="flex flex-col gap-0.5">
                        <span className="text-[11px] font-mono text-indigo-500">
                          #{ticket.ticketNumber}
                        </span>
                        <span className="font-medium text-gray-800 truncate max-w-[200px]">
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
                    <td className="px-4 py-3 text-xs text-gray-500">
                      {ticket.categoryName}
                    </td>
                    <td className="px-4 py-3">
                      {ticket.isSLABreached ? (
                        <span className="flex items-center gap-1 text-xs font-semibold
                                         text-red-700 bg-red-100 px-2 py-0.5 rounded-full w-fit">
                          <AlertTriangle size={11} /> Breached
                        </span>
                      ) : (
                        <span className="flex items-center gap-1 text-xs text-green-600">
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

export default CustomerDashboardPage;