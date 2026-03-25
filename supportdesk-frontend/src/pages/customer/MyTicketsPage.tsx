/**
 * MyTicketsPage — shows all tickets for logged in customer.
 *
 * CONCEPTS:
 *
 * 1. useQuery — fetches and caches ticket data.
 *    queryKey includes filters — refetches when filters change 
 *
 * 2. useState for filters — local state for status/priority filters.
 *    Updates queryKey → triggers new API call automatically 
 *
 * 3. Conditional rendering — shows loading, empty, or ticket list.
 *
 * 4. Date formatting — converts UTC date to readable format.
 */
import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { Plus, Ticket, AlertCircle, Clock, CheckCircle, XCircle } from 'lucide-react';
import Layout from '../../components/common/Layout';
import { getMyTicketsApi } from '../../api/ticketApi';
import { useSearchParams } from 'react-router-dom';

/**
 * Status badge colors — maps status to Tailwind classes.
 * CONCEPT: Object lookup vs switch statement.
 * Object lookup is cleaner and more maintainable 
 */
const statusConfig: Record<string, {
  label: string;
  color: string;
  icon: React.ReactNode
}> = {
  Open: {
    label: 'Open',
    color: 'bg-blue-100 text-blue-700',
    icon: <Ticket size={12} />
  },
  InProgress: {
    label: 'In Progress',
    color: 'bg-yellow-100 text-yellow-700',
    icon: <Clock size={12} />
  },
  OnHold:{
    label: 'On Hold',
    color: 'bg-gray-100 text-gray-700',
    icon: <AlertCircle size={12} />
  },
  Resolved: {
    label: 'Resolved',
    color: 'bg-green-100 text-green-700',
    icon: <CheckCircle size={12} />
  },
  Closed: {
    label: 'Closed',
    color: 'bg-gray-100 text-gray-700',
    icon: <XCircle size={12} />
  },
};

const priorityConfig: Record<string, string> = {
  Low: 'bg-gray-100 text-gray-600',
  Medium: 'bg-blue-100 text-blue-600',
  High: 'bg-orange-100 text-orange-600',
  Critical: 'bg-red-100 text-red-600',
};

 /**
 * CONCEPT: Status string → int map
 * Filter tabs show string labels ("Open", "Resolved")
 * API expects int values (1, 4).
 * This map converts between them 
 */
//Ticket status
const statusMap: Record<string, number> = {
    Open: 1,
    InProgress: 2,
    OnHold:3,
    Resolved: 4,
    Closed: 5    
    };
    
      /**
   * Reverse map — int → string
   * Used to initialize filter tab from URL param ?status=4 → "Resolved" 
   */
  const statusReverseMap: Record<number, string> = {
    1: 'Open',
    2: 'InProgress',
    3: 'OnHold',
    4: 'Resolved',
    5: 'Closed',
  };

const MyTicketsPage: React.FC = () => {
  const navigate = useNavigate();

  /**
   * CONCEPT: useSearchParams reads URL query params.
   * /my-tickets?status=4 → searchParams.get('status') = "4"
   * Convert to int → look up string in reverseMap → "Resolved"
   * Initialize filter tab to "Resolved" automatically 
   */
  const [searchParams] = useSearchParams();
  const initialStatus = searchParams.get('status')
    ? statusReverseMap[Number(searchParams.get('status'))] ?? ''
    : '';

  // Filter state — changing these triggers new API call
  const [statusFilter, setStatusFilter] = useState<string>(initialStatus);
  const [page, setPage] = useState(1);

 
  /**
   * useQuery fetches tickets with current filters.
   * queryKey array includes filters — when filters change,
   * React Query automatically refetches with new params 
   */
 const { data, isLoading, isError } = useQuery({
    queryKey: ['myTickets', page, statusFilter],
    queryFn: () => getMyTicketsApi({
      page,
      pageSize: 10,
      status: statusFilter ? statusMap[statusFilter] : undefined,  
    }),
  });

  const tickets = data?.data?.items ?? [];
  const totalPages = data?.data?.totalPages ?? 1;
  const totalCount = data?.data?.totalCount ?? 0;
 
  // Format date to readable string
  const formatDate = (dateStr: string) =>
    new Date(dateStr).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });

  return (
    <Layout>
      <div className="space-y-6">

        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">
              My Tickets
            </h1>
            <p className="text-gray-500 text-sm mt-1">
              {totalCount} total tickets
            </p>
          </div>

          {/* Create Ticket button */}
          <button
            onClick={() => navigate('/create-ticket')}
            className="flex items-center gap-2 bg-blue-600 text-white
                       px-4 py-2 rounded-lg hover:bg-blue-700
                       transition-colors font-medium"
          >
            <Plus size={18} />
            New Ticket
          </button>
        </div>

        {/* Status Filter Tabs */}
        <div className="flex gap-2 border-b border-gray-200">
          {['', 'Open', 'InProgress', 'OnHold', 'Resolved', 'Closed'].map((status) => (
            <button
              key={status}
              onClick={() => {
                setStatusFilter(status);
                setPage(1); // reset to page 1 on filter change
              }}
              className={`px-4 py-2 text-sm font-medium border-b-2
                         transition-colors ${
                statusFilter === status
                  ? 'border-blue-600 text-blue-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              {status === '' ? 'All' : status === 'InProgress' ? 'In Progress' : status}
            </button>
          ))}
        </div>

        {/* Loading state */}
        {isLoading && (
          <div className="flex items-center justify-center py-12">
            <div className="animate-spin rounded-full h-8 w-8
                           border-b-2 border-blue-600" />
          </div>
        )}

        {/* Error state */}
        {isError && (
          <div className="flex items-center gap-2 text-red-600
                         bg-red-50 p-4 rounded-lg">
            <AlertCircle size={20} />
            <p>Failed to load tickets. Please try again.</p>
          </div>
        )}

        {/* Empty state */}
        {!isLoading && !isError && tickets.length === 0 && (
          <div className="text-center py-12">
            <Ticket size={48} className="mx-auto text-gray-300 mb-4" />
            <h3 className="text-gray-500 font-medium">No tickets found</h3>
            <p className="text-gray-400 text-sm mt-1">
              Create your first ticket to get started
            </p>
            <button
              onClick={() => navigate('/create-ticket')}
              className="mt-4 flex items-center gap-2 bg-blue-600
                         text-white px-4 py-2 rounded-lg
                         hover:bg-blue-700 transition-colors
                         font-medium mx-auto"
            >
              <Plus size={18} />
              New Ticket
            </button>
          </div>
        )}

        {/* Ticket List */}
        {!isLoading && tickets.length > 0 && (
          <div className="space-y-3">
            {tickets.map((ticket) => (
              <div
                key={ticket.id}
                onClick={() => navigate(`/my-tickets/${ticket.id}`)}
                className="bg-white border border-gray-200 rounded-xl
                           p-5 hover:shadow-md hover:border-blue-300
                           transition-all cursor-pointer"
              >
                <div className="flex items-start justify-between gap-4">

                  {/* Left — ticket info */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-1">
                      {/* Ticket number */}
                      <span className="text-xs font-mono text-gray-400">
                        #{ticket.ticketNumber}
                      </span>

                      {/* Status badge */}
                      <span className={`flex items-center gap-1 px-2 py-0.5
                                       text-xs font-medium rounded-full
                                       ${statusConfig[ticket.status]?.color}`}>
                        {statusConfig[ticket.status]?.icon}
                        {statusConfig[ticket.status]?.label}
                      </span>

                      {/* Priority badge */}
                      <span className={`px-2 py-0.5 text-xs font-medium
                                       rounded-full
                                       ${priorityConfig[ticket.priority]}`}>
                        {ticket.priority}
                      </span>

                      {/* SLA breach warning
                      Customer creates ticket → Priority = High
                            SLA Policy for High priority:
                            → First Response   = within 2 hours
                            → Resolution       = within 8 hours

                            SLA Breach happens when:
                            → Agent did NOT respond within 2 hours   ← First Response Breach
                            → Ticket NOT resolved within 8 hours    ← Resolution Breach */}
                      {ticket.isSLABreached && (
                        <span className="flex items-center gap-1 px-2 py-0.5
                                        text-xs font-medium rounded-full
                                        bg-red-100 text-red-600">
                          <AlertCircle size={10} />
                          SLA Breached
                        </span>
                      )}
                    </div>

                    {/* Title */}
                    <h3 className="font-medium text-gray-900 truncate">
                      {ticket.title}
                    </h3>

                    {/* Category + Agent */}
                    <div className="flex items-center gap-3 mt-1">
                      <span className="text-xs text-gray-500">
                        {ticket.categoryName}
                      </span>
                      {ticket.assignedAgentName && (
                        <span className="text-xs text-gray-500">
                          Agent: {ticket.assignedAgentName}
                        </span>
                      )}
                    </div>
                  </div>

                  {/* Right — date */}
                  <div className="text-right shrink-0">
                    <p className="text-xs text-gray-400">
                      {formatDate(ticket.createdAt)}
                    </p>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="flex items-center justify-center gap-2 pt-4">
            <button
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={page === 1}
              className="px-4 py-2 text-sm border border-gray-300
                         rounded-lg disabled:opacity-50
                         hover:bg-gray-50 transition-colors"
            >
              Previous
            </button>
            <span className="text-sm text-gray-600">
              Page {page} of {totalPages}
            </span>
            <button
              onClick={() => setPage(p => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
              className="px-4 py-2 text-sm border border-gray-300
                         rounded-lg disabled:opacity-50
                         hover:bg-gray-50 transition-colors"
            >
              Next
            </button>
          </div>
        )}
      </div>
    </Layout>
  );
};

export default MyTicketsPage;
