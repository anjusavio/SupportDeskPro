/**
 * TicketsPage — Admin and Agent view of all tickets in the tenant.
 *
 * CONCEPTS:
 *
 * 1. useQuery with filter params
 *    Filters passed as query params to GET /api/tickets.
 *    Every time a filter changes → queryKey changes → fresh fetch 
 *
 * 2. Controlled filter state
 *    useState for each filter (status, priority, search etc).
 *    Passed into queryKey array so React Query re-fetches on change 
 *
 * 3. Pagination
 *    Backend returns PagedResult<TicketResponse> with totalCount.
 *    Page state controls which page is fetched 
 *
 * 4. Role-based UI
 *    Agent sees their assigned tickets + all tickets.
 *    Admin sees assign button + extra controls.
 *    Both use same endpoint GET /api/tickets 
 *
 * 5. useMutation for PATCH /tickets/{id}/status
 *    Quick status change directly from the list.
 *    On success → invalidate tickets query → list refreshes 
 *
 * 6. useMutation for PATCH /tickets/{id}/assign
 *    Admin assigns ticket to an agent.
 *    Only shown when user.role === 'Admin' 
 */

import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import {
  Search, Filter, ChevronLeft, ChevronRight,
  AlertTriangle, Circle, Activity, PauseCircle,
  CheckCircle2, XCircle, Clock, User, RefreshCw,
} from 'lucide-react';
import Layout from '../../components/common/Layout';
import axiosClient from '../../api/axiosClient';
import useAuthStore from '../../store/authStore';
import { ApiResponse } from '../../types/api.types';


interface TicketResponse {
  id: string;
  ticketNumber: string;
  title: string;
  status: string;             // "Open" | "InProgress" | "OnHold" | "Resolved" | "Closed"
  priority: string;           // "Low" | "Medium" | "High" | "Critical"
  categoryName: string;
  assignedAgentName: string | null;
  createdByName: string;
  createdAt: string;
  isSLABreached: boolean;
  slaResolutionDueAt: string | null;
}

/**
 * PagedResult — backend wraps list responses in this shape.
 * totalCount used to calculate total pages for pagination 
 */
interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

interface AgentOption {
  id: string;
  fullName: string;
}

// CONFIG MAPS — same as TicketDetailPage for consistency
const STATUS_CONFIG: Record<string, {
  label: string; color: string; icon: React.ElementType; value: number;
}> = {
  Open:       { label: 'Open',        color: 'bg-blue-100 text-blue-700',     icon: Circle,      value: 1 },
  InProgress: { label: 'In Progress', color: 'bg-yellow-100 text-yellow-700', icon: Activity,    value: 2 },
  OnHold:     { label: 'On Hold',     color: 'bg-orange-100 text-orange-700', icon: PauseCircle, value: 3 },
  Resolved:   { label: 'Resolved',    color: 'bg-green-100 text-green-700',   icon: CheckCircle2,value: 4 },
  Closed:     { label: 'Closed',      color: 'bg-gray-100 text-gray-600',     icon: XCircle,     value: 5 },
};

const PRIORITY_CONFIG: Record<string, { label: string; color: string; value: number }> = {
  Low:      { label: 'Low',      color: 'bg-gray-100 text-gray-600',     value: 1 },
  Medium:   { label: 'Medium',   color: 'bg-blue-100 text-blue-700',     value: 2 },
  High:     { label: 'High',     color: 'bg-orange-100 text-orange-700', value: 3 },
  Critical: { label: 'Critical', color: 'bg-red-100 text-red-700',       value: 4 },
};

// HELPERS
function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleString('en-US', {
    month: 'short', day: 'numeric', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  });
}

// SUB-COMPONENT: StatusBadge
function StatusBadge({ status }: { status: string }) {
  const cfg = STATUS_CONFIG[status] ?? { label: status, color: 'bg-gray-100 text-gray-600', icon: Circle };
  const Icon = cfg.icon;
  return (
    <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold ${cfg.color}`}>
      <Icon size={11} />{cfg.label}
    </span>
  );
}

// SUB-COMPONENT: PriorityBadge

function PriorityBadge({ priority }: { priority: string }) {
  const cfg = PRIORITY_CONFIG[priority] ?? { label: priority, color: 'bg-gray-100 text-gray-600' };
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold ${cfg.color}`}>
      {cfg.label}
    </span>
  );
}

// MAIN PAGE COMPONENT

const TicketsPage: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { user } = useAuthStore();
  const isAdmin = user?.role === 'Admin';

  // ─── Filter state
    /**
   * CONCEPT: Each filter is its own state.
   * All are included in queryKey → any change triggers a fresh API call.
   * This is the React Query pattern for server-side filtering 
   */
  const [page, setPage]             = useState(1);
  const [search, setSearch]         = useState('');
  const [statusFilter, setStatusFilter]     = useState<number | ''>('');
  const [priorityFilter, setPriorityFilter] = useState<number | ''>('');
  const [slaBreached, setSlaBreached]       = useState<boolean | ''>('');

  //─── Debounce search — waits 400ms after user stops typing ───────
  useEffect(() => {
    const timer = setTimeout(() => {
      setPage(1);
    }, 400);
    return () => clearTimeout(timer);
  }, [search]);

  // ─── Query: GET /api/tickets (Admin + Agent)
  const {
    data: ticketsData,
    isLoading,
    isError,
    refetch,
  } = useQuery<PagedResult<TicketResponse>>({
    /**
     * CONCEPT: queryKey array with filters
     * Every filter value is in the key.
     * When any filter changes → new key → React Query fetches fresh data ✅
     */
    queryKey: ['tickets', page, search, statusFilter, priorityFilter, slaBreached],
    queryFn: () => {
      // Build query params — only include filters that have values
      const params = new URLSearchParams();
      params.append('page', String(page));
      params.append('pageSize', '20');
      if (search)        params.append('search', search);
      if (statusFilter)  params.append('status', String(statusFilter));
      if (priorityFilter) params.append('priority', String(priorityFilter));
      if (slaBreached !== '') params.append('isSLABreached', String(slaBreached));

      return axiosClient
        .get<ApiResponse<PagedResult<TicketResponse>>>(`/tickets?${params.toString()}`)
        .then((r) => {
          if (!r.data.data) throw new Error('Failed to load tickets');
          return r.data.data; // PagedResult<TicketResponse> 
        });
    },
  });

  const tickets = ticketsData?.items ?? [];
  const totalPages = ticketsData?.totalPages ?? 1;
  const totalCount = ticketsData?.totalCount ?? 0;

  // ─── Mutation: PATCH /tickets/{id}/status 
  const statusMutation = useMutation({
    mutationFn: ({ ticketId, status }: { ticketId: string; status: string }) =>
      axiosClient.patch(`/tickets/${ticketId}/status`, { status }),
    onSuccess: () => {
      toast.success('Status updated');
      // Invalidate so list refreshes with new status 
      queryClient.invalidateQueries({ queryKey: ['tickets'] });
    },
    onError: () => toast.error('Failed to update status'),
  });

  // ─── Mutation: PATCH /tickets/{id}/assign (Admin only) 
  const assignMutation = useMutation({
    mutationFn: ({ ticketId, agentId }: { ticketId: string; agentId: string | null }) =>
      axiosClient.patch(`/tickets/${ticketId}/assign`, { agentId }),
    onSuccess: () => {
      toast.success('Ticket assigned');
      queryClient.invalidateQueries({ queryKey: ['tickets'] });
    },
    onError: () => toast.error('Failed to assign ticket'),
  });



  // ─── Reset all filters 
  function resetFilters() {
    setSearch('');
    setStatusFilter('');
    setPriorityFilter('');
    setSlaBreached('');
    setPage(1);
  }


  // RENDER
  
  return (
    <Layout>
      <div className="space-y-5">

        {/* ── PAGE HEADER ── */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">
              {isAdmin ? 'All Tickets' : 'My Queue'}
            </h1>
            <p className="text-sm text-gray-500 mt-1">
              {totalCount} ticket{totalCount !== 1 ? 's' : ''} found
            </p>
          </div>
          <button
            onClick={() => refetch()}
            className="flex items-center gap-2 px-3 py-2 text-sm text-gray-600
                       border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
          >
            <RefreshCw size={14} /> Refresh
          </button>
        </div>

        {/* ── FILTERS BAR ── */}
        <div className="bg-white border border-gray-200 rounded-xl p-4 shadow-sm">
          <div className="flex flex-wrap items-center gap-3">

            {/* Search input — fires on Enter */}
            <div className="relative flex-1 min-w-[200px]">
              <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
              <input
                type="text"
                value={search}
                 onChange={(e) => {
                        setSearch(e.target.value); // update search directly on every keystroke 
                      }}
                placeholder="Search tickets..."
                className="w-full pl-9 pr-4 py-2 text-sm border border-gray-200 rounded-lg
                           focus:outline-none focus:ring-2 focus:ring-indigo-300"
              />
            </div>

            {/* Status filter */}
            <select
              value={statusFilter}
              onChange={(e) => { setStatusFilter(e.target.value === '' ? '' : Number(e.target.value)); setPage(1); }}
              className="px-3 py-2 text-sm border border-gray-200 rounded-lg bg-white
                         focus:outline-none focus:ring-2 focus:ring-indigo-300"
            >
              <option value="">All Statuses</option>
              {Object.entries(STATUS_CONFIG).map(([key, cfg]) => (
                <option key={key} value={cfg.value}>{cfg.label}</option>
              ))}
            </select>

            {/* Priority filter */}
            <select
              value={priorityFilter}
              onChange={(e) => { setPriorityFilter(e.target.value === '' ? '' : Number(e.target.value)); setPage(1); }}
              className="px-3 py-2 text-sm border border-gray-200 rounded-lg bg-white
                         focus:outline-none focus:ring-2 focus:ring-indigo-300"
            >
              <option value="">All Priorities</option>
              {Object.entries(PRIORITY_CONFIG).map(([key, cfg]) => (
                <option key={key} value={cfg.value}>{cfg.label}</option>
              ))}
            </select>

            {/* SLA breached filter */}
            <select
              value={slaBreached === '' ? '' : String(slaBreached)}
              onChange={(e) => { setSlaBreached(e.target.value === '' ? '' : e.target.value === 'true'); setPage(1); }}
              className="px-3 py-2 text-sm border border-gray-200 rounded-lg bg-white
                         focus:outline-none focus:ring-2 focus:ring-indigo-300"
            >
              <option value="">All SLA</option>
              <option value="true">SLA Breached</option>
              <option value="false">SLA OK</option>
            </select>

            {/* Reset filters button */}
            {(search || statusFilter !== '' || priorityFilter !== '' || slaBreached !== '') && (
              <button
                onClick={resetFilters}
                className="px-3 py-2 text-xs text-red-600 border border-red-200
                           rounded-lg hover:bg-red-50 transition-colors"
              >
                Clear filters
              </button>
            )}
          </div>
        </div>

        {/* ── TICKETS TABLE ── */}
        <div className="bg-white border border-gray-200 rounded-xl shadow-sm overflow-hidden">

          {/* Loading state */}
          {isLoading && (
            <div className="flex items-center justify-center h-48">
              <div className="flex flex-col items-center gap-3 text-gray-400">
                <div className="animate-spin rounded-full h-7 w-7 border-b-2 border-indigo-600" />
                <span className="text-sm">Loading tickets...</span>
              </div>
            </div>
          )}

          {/* Error state */}
          {isError && (
            <div className="flex flex-col items-center justify-center h-48 gap-3 text-gray-500">
              <p className="text-sm">Failed to load tickets.</p>
              <button onClick={() => refetch()}
                className="text-sm text-indigo-600 hover:underline">
                Try again
              </button>
            </div>
          )}

          {/* Empty state */}
          {!isLoading && !isError && tickets.length === 0 && (
            <div className="flex flex-col items-center justify-center h-48 gap-2 text-gray-400">
              <Filter size={32} className="opacity-30" />
              <p className="text-sm">No tickets found.</p>
              {(search || statusFilter !== '' || priorityFilter !== '') && (
                <button onClick={resetFilters}
                  className="text-xs text-indigo-600 hover:underline">
                  Clear filters
                </button>
              )}
            </div>
          )}

          {/* Table */}
          {!isLoading && !isError && tickets.length > 0 && (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-100 bg-gray-50 text-left">
                  <th className="px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                    Ticket
                  </th>
                  <th className="px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                    Status
                  </th>
                  <th className="px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                    Priority
                  </th>
                  <th className="px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                    Category
                  </th>
                  <th className="px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                    Assigned To
                  </th>
                  <th className="px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                    Created
                  </th>
                  <th className="px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                    SLA
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {tickets.map((ticket) => (
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
                        <span className="font-medium text-gray-800 truncate max-w-[260px]">
                          {ticket.title}
                        </span>
                        <span className="text-[11px] text-gray-400">
                          by {ticket.createdByName}
                        </span>
                      </div>
                    </td>

                    {/* Status badge */}
                    <td className="px-4 py-3">
                      <StatusBadge status={ticket.status} />
                    </td>

                    {/* Priority badge */}
                    <td className="px-4 py-3">
                      <PriorityBadge priority={ticket.priority} />
                    </td>

                    {/* Category */}
                    <td className="px-4 py-3 text-gray-600 text-xs">
                      {ticket.categoryName}
                    </td>

                    {/* Assigned agent */}
                    <td className="px-4 py-3">
                      {ticket.assignedAgentName
                        ? <span className="flex items-center gap-1 text-xs text-gray-600">
                            <User size={11} className="text-gray-400" />
                            {ticket.assignedAgentName}
                          </span>
                        : <span className="text-xs text-gray-400 italic">Unassigned</span>
                      }
                    </td>

                    {/* Created date */}
                    <td className="px-4 py-3 text-xs text-gray-500">
                      {formatDate(ticket.createdAt)}
                    </td>

                    {/* SLA breach indicator */}
                    <td className="px-4 py-3" onClick={(e) => e.stopPropagation()}>
                      {ticket.isSLABreached ? (
                        <span className="flex items-center gap-1 text-xs font-semibold
                                         text-red-700 bg-red-100 px-2 py-0.5 rounded-full">
                          <AlertTriangle size={11} /> Breached
                        </span>
                      ) : ticket.slaResolutionDueAt ? (
                        <span className="flex items-center gap-1 text-xs text-amber-600
                                         bg-amber-50 px-2 py-0.5 rounded-full">
                          <Clock size={11} />
                          {new Date(ticket.slaResolutionDueAt) > new Date()
                            ? 'On track'
                            : 'Due soon'}
                        </span>
                      ) : (
                        <span className="text-xs text-gray-400">—</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        {/* ── PAGINATION ── */}
        {totalPages > 1 && (
          <div className="flex items-center justify-between px-1">
            <p className="text-xs text-gray-500">
              Page {page} of {totalPages} · {totalCount} total tickets
            </p>
            <div className="flex items-center gap-2">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
                className="p-1.5 rounded-lg border border-gray-200 hover:bg-gray-50
                           disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
              >
                <ChevronLeft size={15} />
              </button>

              {/* Page number buttons */}
              {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                const pageNum = Math.max(1, Math.min(page - 2, totalPages - 4)) + i;
                return (
                  <button
                    key={pageNum}
                    onClick={() => setPage(pageNum)}
                    className={`w-8 h-8 text-xs rounded-lg border transition-colors ${
                      pageNum === page
                        ? 'bg-indigo-600 text-white border-indigo-600'
                        : 'border-gray-200 hover:bg-gray-50 text-gray-600'
                    }`}
                  >
                    {pageNum}
                  </button>
                );
              })}

              <button
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                disabled={page === totalPages}
                className="p-1.5 rounded-lg border border-gray-200 hover:bg-gray-50
                           disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
              >
                <ChevronRight size={15} />
              </button>
            </div>
          </div>
        )}

      </div>
    </Layout>
  );
};

export default TicketsPage;
