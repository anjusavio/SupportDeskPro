/**
 * NotificationsPage — all roles see their own notifications.
 *
 * CONCEPTS:
 *
 * 1. useQuery for GET /api/notifications
 *    Paginated list filtered by read/unread status.
 *    Auto-refreshes every 30s to catch new notifications 
 *
 * 2. useQuery for GET /api/notifications/unread-count
 *    Lightweight poll every 30s.
 *    Used to show badge count on bell icon in Navbar 
 *
 * 3. useMutation for PATCH /api/notifications/{id}/read
 *    Marks single notification as read on click.
 *    Invalidates both notifications + unread-count queries 
 *
 * 4. useMutation for PATCH /api/notifications/read-all
 *    Bulk marks all as read.
 *    Single DB operation on backend 
 *
 * 5. Navigate to ticket on click
 *    Each notification has ticketId.
 *    Clicking navigates to /tickets/:ticketId 
 */

import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import {
  Bell, BellOff, CheckCheck, RefreshCw,
  Ticket, ExternalLink,
} from 'lucide-react';
import Layout from '../../components/common/Layout';
import axiosClient from '../../api/axiosClient';
import { ApiResponse } from '../../types/api.types';

// ─────────────────────────────────────────────────────────────────────────────
// TYPES — match NotificationResponse.cs exactly
// ─────────────────────────────────────────────────────────────────────────────

interface NotificationResponse {
  id: string;
  type: string;         // "TicketAssigned" | "NewReply" | "StatusChanged" | "SLABreached"
  title: string;
  message: string;
  ticketId: string | null;
  ticketNumber: number | null;
  isRead: boolean;
  readAt: string | null;
  createdAt: string;
}

interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

interface UnreadCountResponse {
  count: number;
}

// ─────────────────────────────────────────────────────────────────────────────
// CONFIG — notification type → icon color
// ─────────────────────────────────────────────────────────────────────────────

const TYPE_CONFIG: Record<string, { color: string; bg: string }> = {
  TicketAssigned:  { color: 'text-indigo-600', bg: 'bg-indigo-100' },
  NewReply:        { color: 'text-blue-600',   bg: 'bg-blue-100' },
  StatusChanged:   { color: 'text-amber-600',  bg: 'bg-amber-100' },
  SLABreached:     { color: 'text-red-600',    bg: 'bg-red-100' },
  TicketCreated:   { color: 'text-green-600',  bg: 'bg-green-100' },
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

/**
 * CONCEPT: Relative time
 * Shows "2 hours ago" instead of full date for recent notifications.
 * Falls back to full date for older ones 
 */
function timeAgo(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const mins  = Math.floor(diff / 60_000);
  const hours = Math.floor(diff / 3_600_000);
  const days  = Math.floor(diff / 86_400_000);
  if (mins < 1)   return 'Just now';
  if (mins < 60)  return `${mins}m ago`;
  if (hours < 24) return `${hours}h ago`;
  if (days < 7)   return `${days}d ago`;
  return formatDate(dateStr);
}

// ─────────────────────────────────────────────────────────────────────────────
// MAIN PAGE COMPONENT
// ─────────────────────────────────────────────────────────────────────────────

const NotificationsPage: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  // ─── Filter state ────────────────────────────────────────────────
  const [page, setPage]           = useState(1);
  const [isReadFilter, setIsReadFilter] = useState<string>(''); // '' | 'true' | 'false'

  // ─── Query: GET /api/notifications ──────────────────────────────
  const {
    data: notificationsData,
    isLoading,
    isError,
    refetch,
  } = useQuery<PagedResult<NotificationResponse>>({
    queryKey: ['notifications', page, isReadFilter],
    queryFn: () => {
      const params = new URLSearchParams();
      params.append('page', String(page));
      params.append('pageSize', '20');
      if (isReadFilter !== '') params.append('isRead', isReadFilter);

      return axiosClient
        .get<ApiResponse<PagedResult<NotificationResponse>>>(
          `/notifications?${params.toString()}`
        )
        .then((r) => {
          if (!r.data.data) throw new Error('Failed to load notifications');
          return r.data.data;
        });
    },
    refetchInterval: 30_000, // poll every 30s for new notifications 
  });

  // ─── Query: GET /api/notifications/unread-count ──────────────────
  const { data: unreadData } = useQuery<UnreadCountResponse>({
    queryKey: ['unread-count'],
    queryFn: () =>
      axiosClient
        .get<ApiResponse<UnreadCountResponse>>('/notifications/unread-count')
        .then((r) => r.data.data ?? { count: 0 }),
    refetchInterval: 30_000,
  });

  const unreadCount = unreadData?.count ?? 0;

  // ─── Mutation: PATCH /api/notifications/{id}/read ────────────────
  const markReadMutation = useMutation({
    mutationFn: (notificationId: string) =>
      axiosClient.patch(`/notifications/${notificationId}/read`),
    onSuccess: () => {
      // Refresh both queries — list + bell badge 
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['unread-count'] });
    },
    onError: () => toast.error('Failed to mark as read'),
  });

  // ─── Mutation: PATCH /api/notifications/read-all ─────────────────
  const markAllReadMutation = useMutation({
    mutationFn: () => axiosClient.patch('/notifications/read-all'),
    onSuccess: () => {
      toast.success('All notifications marked as read');
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['unread-count'] });
    },
    onError: () => toast.error('Failed to mark all as read'),
  });

  // ─── Derived values ──────────────────────────────────────────────
  const notifications = notificationsData?.items      ?? [];
  const totalPages    = notificationsData?.totalPages ?? 1;
  const totalCount    = notificationsData?.totalCount ?? 0;

  /**
   * CONCEPT: Handle notification click
   * 1. Mark as read if not already read
   * 2. Navigate to ticket if ticketId exists 
   */
  function handleNotificationClick(notification: NotificationResponse) {
    if (!notification.isRead) {
      markReadMutation.mutate(notification.id);
    }
    if (notification.ticketId) {
      navigate(`/tickets/${notification.ticketId}`);
    }
  }

  // ─────────────────────────────────────────────────────────────────
  // RENDER
  // ─────────────────────────────────────────────────────────────────

  return (
    <Layout>
      <div className="max-w-3xl mx-auto space-y-5">

        {/* ── PAGE HEADER ── */}
        <div className="flex items-center justify-between">
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-2xl font-bold text-gray-900">Notifications</h1>
              {/* Unread badge */}
              {unreadCount > 0 && (
                <span className="bg-red-500 text-white text-xs font-bold
                                 px-2 py-0.5 rounded-full">
                  {unreadCount}
                </span>
              )}
            </div>
            <p className="text-sm text-gray-500 mt-1">
              {totalCount} notification{totalCount !== 1 ? 's' : ''}
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

            {/* Mark all read — only shows if unread exist */}
            {unreadCount > 0 && (
              <button
                onClick={() => markAllReadMutation.mutate()}
                disabled={markAllReadMutation.isPending}
                className="flex items-center gap-2 px-3 py-2 text-sm font-medium
                           text-indigo-600 border border-indigo-200 rounded-lg
                           hover:bg-indigo-50 transition-colors
                           disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <CheckCheck size={14} />
                {markAllReadMutation.isPending ? 'Marking...' : 'Mark all read'}
              </button>
            )}
          </div>
        </div>

        {/* ── FILTER TABS — All / Unread / Read ── */}
        <div className="flex items-center gap-1 bg-gray-100 p-1 rounded-xl w-fit">
          {[
            { label: 'All',    value: '' },
            { label: 'Unread', value: 'false' },
            { label: 'Read',   value: 'true' },
          ].map((tab) => (
            <button
              key={tab.value}
              onClick={() => { setIsReadFilter(tab.value); setPage(1); }}
              className={`px-4 py-1.5 text-sm font-medium rounded-lg transition-colors ${
                isReadFilter === tab.value
                  ? 'bg-white text-gray-800 shadow-sm'
                  : 'text-gray-500 hover:text-gray-700'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {/* ── NOTIFICATIONS LIST ── */}
        <div className="bg-white border border-gray-200 rounded-xl shadow-sm overflow-hidden">

          {/* Loading */}
          {isLoading && (
            <div className="flex items-center justify-center h-48">
              <div className="flex flex-col items-center gap-3 text-gray-400">
                <div className="animate-spin rounded-full h-7 w-7 border-b-2 border-indigo-600" />
                <span className="text-sm">Loading notifications...</span>
              </div>
            </div>
          )}

          {/* Error */}
          {isError && (
            <div className="flex flex-col items-center justify-center h-48 gap-3 text-gray-500">
              <p className="text-sm">Failed to load notifications.</p>
              <button onClick={() => refetch()}
                className="text-sm text-indigo-600 hover:underline">
                Try again
              </button>
            </div>
          )}

          {/* Empty */}
          {!isLoading && !isError && notifications.length === 0 && (
            <div className="flex flex-col items-center justify-center h-48 gap-2 text-gray-400">
              <BellOff size={32} className="opacity-30" />
              <p className="text-sm">
                {isReadFilter === 'false' ? 'No unread notifications' : 'No notifications yet'}
              </p>
            </div>
          )}

          {/* Notification rows */}
          {!isLoading && !isError && notifications.length > 0 && (
            <div className="divide-y divide-gray-50">
              {notifications.map((notification) => {
                const typeCfg = TYPE_CONFIG[notification.type] ?? {
                  color: 'text-gray-500', bg: 'bg-gray-100',
                };
                return (
                  <div
                    key={notification.id}
                    onClick={() => handleNotificationClick(notification)}
                    className={`flex items-start gap-4 px-5 py-4 cursor-pointer
                                hover:bg-gray-50 transition-colors ${
                      !notification.isRead ? 'bg-indigo-50/40' : ''
                    }`}
                  >
                    {/* Icon */}
                    <div className={`h-9 w-9 shrink-0 rounded-full flex items-center
                                     justify-center ${typeCfg.bg}`}>
                      <Bell size={15} className={typeCfg.color} />
                    </div>

                    {/* Content */}
                    <div className="flex-1 min-w-0">
                      <div className="flex items-start justify-between gap-2">
                        <div className="flex-1">
                          <p className={`text-sm font-medium ${
                            !notification.isRead ? 'text-gray-900' : 'text-gray-600'
                          }`}>
                            {notification.title}
                          </p>
                          <p className="text-xs text-gray-500 mt-0.5 leading-relaxed">
                            {notification.message}
                          </p>

                          {/* Ticket number link */}
                          {notification.ticketNumber && (
                            <span className="inline-flex items-center gap-1 mt-1.5
                                             text-xs text-indigo-600 font-medium">
                              <Ticket size={11} />
                              #{notification.ticketNumber}
                              <ExternalLink size={10} />
                            </span>
                          )}
                        </div>

                        {/* Right side: time + unread dot */}
                        <div className="flex flex-col items-end gap-1.5 shrink-0">
                          <span className="text-[11px] text-gray-400">
                            {timeAgo(notification.createdAt)}
                          </span>
                          {/* Unread blue dot */}
                          {!notification.isRead && (
                            <div className="w-2 h-2 rounded-full bg-indigo-500" />
                          )}
                        </div>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>

        {/* ── PAGINATION ── */}
        {totalPages > 1 && (
          <div className="flex items-center justify-between px-1">
            <p className="text-xs text-gray-500">
              Page {page} of {totalPages}
            </p>
            <div className="flex items-center gap-2">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
                className="px-3 py-1.5 text-xs border border-gray-200 rounded-lg
                           hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed"
              >
                Previous
              </button>
              <button
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                disabled={page === totalPages}
                className="px-3 py-1.5 text-xs border border-gray-200 rounded-lg
                           hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed"
              >
                Next
              </button>
            </div>
          </div>
        )}

      </div>
    </Layout>
  );
};

export default NotificationsPage;