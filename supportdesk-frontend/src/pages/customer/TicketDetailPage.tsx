/**
 * TicketDetailPage — full ticket view with conversation thread,
 * SLA timers, status history timeline, and reply/internal-note form.
 *
 * CONCEPTS:
 *
 * 1. useParams — reads :id from the URL
 *    /tickets/42 → id = "42"
 *    Used to build API URLs dynamically 
 *
 * 2. Multiple useQuery calls on one page
 *    Three separate queries run in parallel:
 *    - ticket detail   GET /api/tickets/:id
 *    - comments        GET /api/tickets/:id/comments
 *    - status history  GET /api/tickets/:id/status-history
 *    - agents          GET /api/users/agents (Admin only)
 *    React Query fires all simultaneously 
 *
 * 3. enabled: !!ticket — dependent query
 *    Comments and history only fetch AFTER ticket is loaded.
 *    !! converts object to boolean (null → false, object → true) 
 *
 * 4. Role-based rendering
 *    Same page used by Customer, Agent, Admin.
 *    Internal notes hidden from Customer.
 *    Status-change dropdown only shows for Agent/Admin.
 *    Assign dropdown only shows for Admin 
 *
 * 5. useMutation for POST /comments
 *    Sends JSON body { body, isInternal } to backend.
 *    On success → refetch comments → thread refreshes 
 *
 * 6. Live SLA countdown timer
 *    useEffect + setInterval ticks every second.
 *    Calculates remaining time from dueAt timestamp.
 *    Cleanup function stops timer on unmount → no memory leak 
 *
 * 7. Custom toggle switch (isInternal)
 *    watch('isInternal') reads current form value.
 *    setValue('isInternal', ...) updates it on click.
 *    Amber styling applied when internal mode is active 
 *
 * 8. Click-outside to close dropdown
 *    useRef + document.addEventListener('mousedown')
 *    Detects click outside the dropdown div and closes it 
 */

import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import {
  ArrowLeft, Clock, User, Tag, AlertTriangle,
  CheckCircle2, Circle, PauseCircle, XCircle,
  Send, Paperclip, Lock, Unlock, ChevronDown,
  Download, Calendar, Activity, FileText, Image, File,
} from 'lucide-react';
import Layout from '../../components/common/Layout';
import axiosClient from '../../api/axiosClient';
import useAuthStore from '../../store/authStore';
import { ApiResponse } from '../../types/api.types';

// ─────────────────────────────────────────────────────────────────────────────
// TYPES — match exactly what your .NET API returns
// ─────────────────────────────────────────────────────────────────────────────

interface TicketDetail {
  id: string;
  ticketNumber: string;
  title: string;
  description: string;
  status: string;
  priority: string;
  categoryName: string;
  assignedAgentId: string | null;
  assignedAgentName: string | null;
  createdByName: string;
  createdAt: string;
  updatedAt: string;
  slaFirstResponseDueAt: string | null;
  slaResolutionDueAt: string | null;
  isSLABreached: boolean;
  firstRespondedAt: string | null;
  resolvedAt: string | null;
}

interface TicketComment {
  id: string;
  body: string;
  isInternal: boolean;
  authorName: string;
  authorRole: string;
  createdAt: string;
  attachments: CommentAttachment[] | null; // null when no attachments
}

interface CommentAttachment {
  id: string;
  fileName: string;
  fileSize: number;
  contentType: string;
  downloadUrl: string;
}

interface StatusHistory {
  id: string;
  fromStatus: string;
  toStatus: string;
  changedByName: string;
  changedAt: string;
  note: string | null;
}

interface AgentSummaryResponse {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
}

// ─────────────────────────────────────────────────────────────────────────────
// ZOD SCHEMA — reply / internal note form
// ─────────────────────────────────────────────────────────────────────────────

const commentSchema = z.object({
  body: z.string().min(1, 'Reply cannot be empty').max(5000),
  isInternal: z.boolean(),
});
type CommentFormData = z.infer<typeof commentSchema>;

// ─────────────────────────────────────────────────────────────────────────────
// CONFIG MAPS — defined outside component so they don't re-create on render
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

/**
 * CONCEPT: Status transition rules
 * number values match backend TicketStatus enum:
 * Open=1, InProgress=2, OnHold=3, Resolved=4, Closed=5
 */
const ALLOWED_TRANSITIONS: Record<string, number[]> = {
  Open:       [2, 3, 5],
  InProgress: [3, 4, 5],
  OnHold:     [2, 5],
  Resolved:   [5, 1],
  Closed:     [],
};

const STATUS_NUMBER_MAP: Record<number, { label: string; icon: React.ElementType }> = {
  1: { label: 'Open',        icon: Circle },
  2: { label: 'In Progress', icon: Activity },
  3: { label: 'On Hold',     icon: PauseCircle },
  4: { label: 'Resolved',    icon: CheckCircle2 },
  5: { label: 'Closed',      icon: XCircle },
};

// ─────────────────────────────────────────────────────────────────────────────
// HELPERS — defined outside component
// ─────────────────────────────────────────────────────────────────────────────

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleString('en-US', {
    month: 'short', day: 'numeric', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  });
}

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function getFileIcon(contentType: string): React.ElementType {
  if (contentType.startsWith('image/')) return Image;
  if (contentType === 'application/pdf') return FileText;
  return File;
}

// ─────────────────────────────────────────────────────────────────────────────
// SUB-COMPONENT: SLATimer
// ─────────────────────────────────────────────────────────────────────────────

function SLATimer({ dueAt, label }: { dueAt: string; label: string }) {
  const [remaining, setRemaining] = useState('');
  const [isBreached, setIsBreached] = useState(false);

  useEffect(() => {
    function tick() {
      const diff = new Date(dueAt).getTime() - Date.now();
      if (diff <= 0) {
        setIsBreached(true);
        setRemaining('Breached');
        return;
      }
      const h = Math.floor(diff / 3_600_000);
      const m = Math.floor((diff % 3_600_000) / 60_000);
      const s = Math.floor((diff % 60_000) / 1_000);
      setRemaining(h > 0 ? `${h}h ${m}m` : m > 0 ? `${m}m ${s}s` : `${s}s`);
    }
    tick();
    const id = setInterval(tick, 1000);
    return () => clearInterval(id); // cleanup → no memory leak 
  }, [dueAt]);

  return (
    <div className={`flex items-center gap-1.5 text-xs font-medium px-2.5 py-1.5 rounded-full ${
      isBreached ? 'bg-red-100 text-red-700' : 'bg-amber-50 text-amber-700'
    }`}>
      <Clock size={12} />
      <span>{label}: {remaining}</span>
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
    <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold ${cfg.color}`}>
      <Icon size={12} />
      {cfg.label}
    </span>
  );
}

function PriorityBadge({ priority }: { priority: string }) {
  const cfg = PRIORITY_CONFIG[priority] ?? { label: priority, color: 'bg-gray-100 text-gray-600' };
  return (
    <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-xs font-semibold ${cfg.color}`}>
      {cfg.label}
    </span>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// SUB-COMPONENT: StatusChangeDropdown
// ─────────────────────────────────────────────────────────────────────────────

function StatusChangeDropdown({
  currentStatus, ticketId, onChanged,
}: {
  currentStatus: string; ticketId: string; onChanged: () => void;
}) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const transitions = ALLOWED_TRANSITIONS[currentStatus] ?? [];

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const mutation = useMutation({
    mutationFn: (newStatus: number) =>
      axiosClient.patch(`/tickets/${ticketId}/status`, { status: newStatus }),
    onSuccess: () => { toast.success('Status updated'); onChanged(); setOpen(false); },
    onError: () => toast.error('Failed to update status'),
  });

  if (transitions.length === 0) return null;

  return (
    <div ref={ref} className="relative">
      <button
        onClick={() => setOpen(o => !o)}
        className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium
                   bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
      >
        Change Status <ChevronDown size={12} />
      </button>

      {open && (
        <div className="absolute right-0 mt-1 w-40 bg-white border border-gray-200
                        rounded-xl shadow-lg z-20 overflow-hidden">
          {transitions.map(statusNum => {
            const cfg = STATUS_NUMBER_MAP[statusNum];
            const Icon = cfg?.icon ?? Circle;
            return (
              <button
                key={statusNum}
                onClick={() => mutation.mutate(statusNum)}
                disabled={mutation.isPending}
                className="w-full flex items-center gap-2 px-3 py-2 text-xs
                           text-gray-700 hover:bg-gray-50 transition-colors"
              >
                <Icon size={13} className="opacity-60" />
                {cfg?.label ?? statusNum}
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// SUB-COMPONENT: CommentBubble
// ─────────────────────────────────────────────────────────────────────────────

function CommentBubble({
  comment, currentUserName,
}: {
  comment: TicketComment; currentUserName: string;
}) {
  const isOwn = comment.authorName === currentUserName;
  const isAgent = comment.authorRole === 'Agent' || comment.authorRole === 'Admin';

  return (
    <div className={`flex gap-3 ${isOwn ? 'flex-row-reverse' : ''}`}>
      <div className={`h-8 w-8 shrink-0 rounded-full flex items-center justify-center
                       text-xs font-bold ${
                         isAgent ? 'bg-indigo-100 text-indigo-700' : 'bg-emerald-100 text-emerald-700'
                       }`}>
        {comment.authorName.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2)}
      </div>

      <div className={`flex-1 max-w-2xl flex flex-col gap-1 ${isOwn ? 'items-end' : 'items-start'}`}>
        <div className="flex items-center gap-2 flex-wrap">
          <span className="text-xs font-semibold text-gray-700">{comment.authorName}</span>
          <span className="text-[10px] text-gray-400 bg-gray-100 px-1.5 py-0.5 rounded">
            {comment.authorRole}
          </span>
          {comment.isInternal && (
            <span className="flex items-center gap-1 text-[10px] font-medium text-amber-600
                             bg-amber-50 px-1.5 py-0.5 rounded border border-amber-200">
              <Lock size={9} /> Internal Note
            </span>
          )}
          <span className="text-[10px] text-gray-400">{formatDate(comment.createdAt)}</span>
        </div>

        <div className={`px-4 py-3 rounded-2xl text-sm leading-relaxed ${
          comment.isInternal
            ? 'bg-amber-50 border border-amber-200 text-amber-900'
            : isOwn
              ? 'bg-indigo-600 text-white rounded-tr-sm'
              : 'bg-white border border-gray-200 text-gray-800 rounded-tl-sm shadow-sm'
        }`}>
          <p className="whitespace-pre-wrap">{comment.body}</p>
        </div>

        {(comment.attachments ?? []).length > 0 && (
          <div className="flex flex-wrap gap-2 mt-1">
            {(comment.attachments ?? []).map(att => {
              const Icon = getFileIcon(att.contentType);
              return (
                <div key={att.id}
                  className="flex items-center gap-2 px-3 py-2 bg-gray-50 border
                             border-gray-200 rounded-lg hover:bg-gray-100 transition-colors">
                  <Icon size={14} className="text-gray-500 shrink-0" />
                  <div className="min-w-0">
                    <p className="text-xs font-medium text-gray-700 truncate max-w-[120px]">
                      {att.fileName}
                    </p>
                    <p className="text-[10px] text-gray-400">{formatFileSize(att.fileSize)}</p>
                  </div>
                  <a href={att.downloadUrl} download
                    className="p-1 rounded hover:bg-gray-200 text-gray-500">
                    <Download size={12} />
                  </a>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// SUB-COMPONENT: InfoRow
// ─────────────────────────────────────────────────────────────────────────────

function InfoRow({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex items-start justify-between gap-2">
      <span className="text-xs text-gray-400 shrink-0 pt-0.5">{label}</span>
      <div className="text-right">{children}</div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// SUB-COMPONENT: AssignAgentDropdown
// Shows dropdown + Assign button — does NOT fire API on select change.
// User must click Assign button to confirm 
// ─────────────────────────────────────────────────────────────────────────────

function AssignAgentDropdown({
  currentAgentId, agents, onAssign, isPending, isDisabled,
}: {
  currentAgentId: string | null;
  agents: AgentSummaryResponse[];
  onAssign: (agentId: string | null) => void;
  isPending: boolean;
  isDisabled: boolean;
}) {
  // Local state — tracks selected value before confirming
  const [selectedId, setSelectedId] = useState(currentAgentId ?? '');

  // Sync if ticket data refreshes (e.g. after assign)
  useEffect(() => {
    setSelectedId(currentAgentId ?? '');
  }, [currentAgentId]);

  const hasChanged = selectedId !== (currentAgentId ?? '');

  return (
    <div className="flex flex-col gap-2 items-end">
      <select
        value={selectedId}
        onChange={(e) => setSelectedId(e.target.value)} // only updates local state 
        disabled={isPending || isDisabled}
        className="text-sm border border-gray-200 rounded-lg px-2 py-1
                   focus:outline-none focus:ring-2 focus:ring-indigo-300
                   bg-white disabled:opacity-50 disabled:cursor-not-allowed"
      >
        <option value="">Unassigned</option>
        {agents.map((agent) => (
          <option key={agent.id} value={agent.id}>
            {`${agent.firstName} ${agent.lastName}`}
          </option>
        ))}
      </select>

      {/* Assign button — only shows when selection has changed */}
      {hasChanged && (
        <button
          onClick={() => onAssign(selectedId || null)}
          disabled={isPending || isDisabled}
          className="px-3 py-1 text-xs font-medium bg-indigo-600 text-white
                     rounded-lg hover:bg-indigo-700 transition-colors
                     disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {isPending ? 'Assigning...' : 'Assign ✓'}
        </button>
      )}
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// MAIN PAGE COMPONENT
// ─────────────────────────────────────────────────────────────────────────────

const TicketDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { user } = useAuthStore();

  // ─── 1. Role checks — always first, other hooks depend on these ──────────
  const isAgentOrAdmin =
    user?.role === 'Agent' || user?.role === 'Admin' || user?.role === 'SuperAdmin';
  const isAdmin = user?.role === 'Admin';

  // ─── 2. Local state ──────────────────────────────────────────────────────
  const [showInternal, setShowInternal] = useState(false);
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // ─── 3. React Hook Form ──────────────────────────────────────────────────
  const {
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors },
  } = useForm<CommentFormData>({
    resolver: zodResolver(commentSchema),
    defaultValues: { body: '', isInternal: false },
  });

  const isInternalValue = watch('isInternal');

  // ─── 4. Query: ticket detail ─────────────────────────────────────────────
  const {
    data: ticket,
    isLoading: ticketLoading,
    refetch: refetchTicket,
  } = useQuery<TicketDetail>({
    queryKey: ['ticket', id],
    queryFn: () =>
      axiosClient
        .get<ApiResponse<TicketDetail>>(`/tickets/${id}`)
        .then((r) => {
          if (!r.data.data) throw new Error('Ticket not found');
          return r.data.data;
        }),
    refetchInterval: 30_000,
  });

  // ─── 5. Query: comments ──────────────────────────────────────────────────
  const { data: comments = [], refetch: refetchComments } = useQuery<TicketComment[]>({
    queryKey: ['ticket-comments', id],
    queryFn: () =>
      axiosClient
        .get<ApiResponse<TicketComment[]>>(`/tickets/${id}/comments`)
        .then((r) => r.data.data ?? []),
    enabled: !!ticket, // only after ticket loads 
    refetchInterval: 30_000,
  });

  // ─── 6. Query: status history ────────────────────────────────────────────
  const { data: history = [] } = useQuery<StatusHistory[]>({
    queryKey: ['ticket-history', id],
    queryFn: () =>
      axiosClient
        .get<ApiResponse<StatusHistory[]>>(`/tickets/${id}/history`)
        .then((r) => r.data.data ?? []),
    enabled: !!ticket,
  });

  // ─── 7. Query: agents for assign dropdown (Admin only) ───────────────────
  const { data: agents = [] } = useQuery<AgentSummaryResponse[]>({
    queryKey: ['agents'],
    queryFn: () =>
      axiosClient
        .get<ApiResponse<AgentSummaryResponse[]>>('/users/agents')
        .then((r) => r.data.data ?? []),
    enabled: isAdmin, // only fetch for Admin 
  });

  // ─── 8. Mutation: assign ticket (Admin only) ─────────────────────────────
  const assignMutation = useMutation({
    mutationFn: (agentId: string | null) =>
      axiosClient.patch(`/tickets/${id}/assign`, { agentId }),
    onSuccess: () => {
      toast.success('Ticket assigned successfully');
      refetchTicket();
    },
    onError: () => toast.error('Failed to assign ticket'),
  });

  // ─── 9. Mutation: add comment ────────────────────────────────────────────
  const addCommentMutation = useMutation({
    mutationFn: async (data: CommentFormData) => {
      return axiosClient.post(`/tickets/${id}/comments`, {
        body: data.body,
        isInternal: data.isInternal,
      });
    },
    onSuccess: () => {
      toast.success('Reply sent');
      reset();
      setSelectedFiles([]);
      refetchComments();
      refetchTicket();
    },
    onError: (error: any) => {
      const message = error.response?.data?.message || 'Failed to send reply.';
      toast.error(message);
    },
  });

  function onSubmit(data: CommentFormData) {
    addCommentMutation.mutate(data);
  }

  // ─── 10. File handlers ───────────────────────────────────────────────────
  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const files = Array.from(e.target.files ?? []);
    const valid = files.filter(f => f.size <= 10 * 1024 * 1024);
    if (valid.length < files.length) toast.error('Files larger than 10 MB were skipped');
    setSelectedFiles(prev => [...prev, ...valid]);
    e.target.value = '';
  }

  function removeFile(index: number) {
    setSelectedFiles(prev => prev.filter((_, i) => i !== index));
  }

  // ─── 11. Derived values ──────────────────────────────────────────────────
  /**
   * CONCEPT: Role-scoped comment filtering
   * Customers never see isInternal = true.
   * Agents see all but can toggle amber notes on/off 
   */
  const visibleComments = isAgentOrAdmin
    ? comments.filter(c => !c.isInternal || showInternal)
    : comments.filter(c => !c.isInternal);

  // ─── 12. Loading / error states ──────────────────────────────────────────
  if (ticketLoading) {
    return (
      <Layout>
        <div className="flex items-center justify-center h-96">
          <div className="flex flex-col items-center gap-3 text-gray-500">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600" />
            <span className="text-sm">Loading ticket...</span>
          </div>
        </div>
      </Layout>
    );
  }

  if (!ticket) {
    return (
      <Layout>
        <div className="flex flex-col items-center justify-center h-96 gap-3">
          <p className="text-gray-500">Ticket not found.</p>
          <button onClick={() => navigate(-1)} className="text-sm text-indigo-600 hover:underline">
            Go back
          </button>
        </div>
      </Layout>
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // RENDER
  // ─────────────────────────────────────────────────────────────────────────

  return (
    <Layout>
      <div className="max-w-6xl mx-auto space-y-6">

        {/* ── HEADER ── */}
        <div className="flex items-start gap-4">
          <button
            onClick={() => navigate(-1)}
            className="mt-1 p-1.5 rounded-lg hover:bg-gray-100 text-gray-500 transition-colors"
          >
            <ArrowLeft size={18} />
          </button>

          <div className="flex-1 min-w-0">
            <div className="flex flex-wrap items-center gap-2 mb-1">
              <span className="text-xs font-mono font-semibold text-indigo-600
                               bg-indigo-50 px-2 py-0.5 rounded">
                #{ticket.ticketNumber}
              </span>
              {ticket.isSLABreached && (
                <span className="flex items-center gap-1 text-xs font-semibold
                                 text-red-700 bg-red-100 px-2.5 py-1 rounded-full">
                  <AlertTriangle size={11} /> SLA Breached
                </span>
              )}
              {ticket.slaFirstResponseDueAt && !ticket.firstRespondedAt && (
                <SLATimer dueAt={ticket.slaFirstResponseDueAt} label="First Response" />
              )}
              {ticket.slaResolutionDueAt && !ticket.resolvedAt && (
                <SLATimer dueAt={ticket.slaResolutionDueAt} label="Resolution" />
              )}
            </div>

            <h1 className="text-xl font-bold text-gray-900 leading-tight">{ticket.title}</h1>

            <div className="flex flex-wrap items-center gap-2 mt-2">
              <StatusBadge status={ticket.status} />
              <PriorityBadge priority={ticket.priority} />
              <span className="text-xs text-gray-400 flex items-center gap-1">
                <Tag size={11} /> {ticket.categoryName}
              </span>
              <span className="text-xs text-gray-400 flex items-center gap-1">
                <Calendar size={11} /> {formatDate(ticket.createdAt)}
              </span>
            </div>
          </div>

          {isAgentOrAdmin && (
            <StatusChangeDropdown
              currentStatus={ticket.status}
              ticketId={ticket.id}
              onChanged={() => {
                refetchTicket();
                queryClient.invalidateQueries({ queryKey: ['ticket-history', id] });
              }}
            />
          )}
        </div>

        {/* ── TWO-COLUMN LAYOUT ── */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">

          {/* ── LEFT COLUMN (2/3) ── */}
          <div className="lg:col-span-2 space-y-4">

            {/* Description */}
            <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm">
              <h2 className="text-xs font-semibold text-gray-400 uppercase tracking-wider mb-3">
                Description
              </h2>
              <p className="text-sm text-gray-700 leading-relaxed whitespace-pre-wrap">
                {ticket.description}
              </p>
            </div>

            {/* Conversation thread */}
            <div className="bg-white border border-gray-200 rounded-xl shadow-sm overflow-hidden">
              <div className="flex items-center justify-between px-5 py-4 border-b border-gray-100">
                <h2 className="text-sm font-semibold text-gray-700">
                  Conversation
                  <span className="ml-2 text-xs font-normal text-gray-400">
                    ({visibleComments.length} messages)
                  </span>
                </h2>
                {isAgentOrAdmin && (
                  <button
                    onClick={() => setShowInternal(v => !v)}
                    className={`flex items-center gap-1.5 text-xs px-2.5 py-1 rounded-full transition-colors ${
                      showInternal
                        ? 'bg-amber-100 text-amber-700'
                        : 'bg-gray-100 text-gray-500 hover:bg-gray-200'
                    }`}
                  >
                    {showInternal ? <Lock size={11} /> : <Unlock size={11} />}
                    {showInternal ? 'Hiding' : 'Show'} internal notes
                  </button>
                )}
              </div>

              <div className="p-5 space-y-5">
                {visibleComments.length === 0 ? (
                  <div className="text-center py-10 text-gray-400">
                    <p className="text-sm">No messages yet.</p>
                    <p className="text-xs mt-1">Be the first to reply below.</p>
                  </div>
                ) : (
                  visibleComments.map(comment => (
                    <CommentBubble
                      key={comment.id}
                      comment={comment}
                      currentUserName={`${user?.firstName ?? ''} ${user?.lastName ?? ''}`.trim()}
                    />
                  ))
                )}
              </div>
            </div>

            {/* Reply box */}
            {ticket.status !== 'Closed' ? (
              <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm">
                <h2 className="text-xs font-semibold text-gray-400 uppercase tracking-wider mb-3">
                  {isAgentOrAdmin ? 'Reply / Internal Note' : 'Add Reply'}
                </h2>

                <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
                  {isAgentOrAdmin && (
                    <label className="flex items-center gap-2 cursor-pointer w-fit">
                      <div
                        onClick={() => setValue('isInternal', !isInternalValue)}
                        className={`relative w-9 h-5 rounded-full transition-colors cursor-pointer ${
                          isInternalValue ? 'bg-amber-400' : 'bg-gray-200'
                        }`}
                      >
                        <div className={`absolute top-0.5 left-0.5 w-4 h-4 bg-white rounded-full
                                         shadow transition-transform ${
                                           isInternalValue ? 'translate-x-4' : ''
                                         }`} />
                      </div>
                      <span className="text-xs text-gray-600">
                        {isInternalValue ? '🔒 Internal note (agents only)' : '💬 Visible to customer'}
                      </span>
                    </label>
                  )}

                  <textarea
                    {...register('body')}
                    rows={4}
                    placeholder={
                      isInternalValue
                        ? 'Write an internal note (not visible to customer)...'
                        : 'Write your reply...'
                    }
                    className={`w-full px-4 py-3 text-sm border rounded-xl resize-none
                                focus:outline-none focus:ring-2 transition-colors ${
                      isInternalValue
                        ? 'border-amber-200 bg-amber-50 focus:ring-amber-300 placeholder-amber-300'
                        : 'border-gray-200 bg-gray-50 focus:ring-indigo-300 placeholder-gray-400'
                    } ${errors.body ? 'border-red-300' : ''}`}
                  />
                  {errors.body && (
                    <p className="text-xs text-red-500">{errors.body.message}</p>
                  )}

                  {selectedFiles.length > 0 && (
                    <div className="flex flex-wrap gap-2">
                      {selectedFiles.map((f, i) => (
                        <div key={i}
                          className="flex items-center gap-1.5 px-2.5 py-1.5 bg-indigo-50
                                     border border-indigo-200 rounded-lg">
                          <Paperclip size={11} className="text-indigo-500" />
                          <span className="text-xs text-indigo-700 max-w-[140px] truncate">
                            {f.name}
                          </span>
                          <button
                            type="button"
                            onClick={() => removeFile(i)}
                            className="text-indigo-400 hover:text-indigo-700 text-sm leading-none"
                          >
                            ×
                          </button>
                        </div>
                      ))}
                    </div>
                  )}

                  <div className="flex items-center justify-between">
                    <button
                      type="button"
                      onClick={() => fileInputRef.current?.click()}
                      className="flex items-center gap-1.5 px-3 py-1.5 text-xs text-gray-500
                                 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
                    >
                      <Paperclip size={12} /> Attach files
                    </button>

                    <input
                      ref={fileInputRef}
                      type="file"
                      multiple
                      accept=".jpg,.jpeg,.png,.gif,.pdf,.doc,.docx,.xls,.xlsx,.txt"
                      className="hidden"
                      onChange={handleFileChange}
                    />

                    <button
                      type="submit"
                      disabled={addCommentMutation.isPending}
                      className={`flex items-center gap-2 px-5 py-2 text-sm font-medium rounded-xl
                                  text-white transition-colors disabled:opacity-50
                                  disabled:cursor-not-allowed ${
                        isInternalValue
                          ? 'bg-amber-500 hover:bg-amber-600'
                          : 'bg-indigo-600 hover:bg-indigo-700'
                      }`}
                    >
                      <Send size={13} />
                      {addCommentMutation.isPending
                        ? 'Sending...'
                        : isInternalValue ? 'Add Note' : 'Send Reply'}
                    </button>
                  </div>
                </form>
              </div>
            ) : (
              <div className="text-center py-4 text-sm text-gray-400 bg-gray-50
                              rounded-xl border border-dashed border-gray-200">
                This ticket is closed. Reopen it to add a reply.
              </div>
            )}
          </div>

          {/* ── RIGHT COLUMN (1/3) ── */}
          <div className="space-y-4">

            {/* Ticket Info panel */}
            <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm space-y-4">
              <h2 className="text-xs font-semibold text-gray-400 uppercase tracking-wider">
                Ticket Info
              </h2>
              <InfoRow label="Status"><StatusBadge status={ticket.status} /></InfoRow>
              <InfoRow label="Priority"><PriorityBadge priority={ticket.priority} /></InfoRow>
              <InfoRow label="Category">
                <span className="text-sm text-gray-700">{ticket.categoryName}</span>
              </InfoRow>
              <InfoRow label="Created by">
                <span className="text-sm text-gray-700">{ticket.createdByName}</span>
              </InfoRow>
            <InfoRow label="Assigned to">
              {isAdmin ? (
                <AssignAgentDropdown
                  currentAgentId={ticket.assignedAgentId}
                  agents={agents}
                  onAssign={(agentId) => assignMutation.mutate(agentId)}
                  isPending={assignMutation.isPending}
                  isDisabled={ticket.status === 'Closed'}
                />
              ) : (
                ticket.assignedAgentName
                  ? <span className="flex items-center gap-1.5 text-sm text-gray-700">
                      <User size={13} className="text-gray-400" />
                      {ticket.assignedAgentName}
                    </span>
                  : <span className="text-sm text-gray-400 italic">Unassigned</span>
              )}
            </InfoRow>
              <InfoRow label="Created">
                <span className="text-xs text-gray-500">{formatDate(ticket.createdAt)}</span>
              </InfoRow>
              {ticket.resolvedAt && (
                <InfoRow label="Resolved">
                  <span className="text-xs text-gray-500">{formatDate(ticket.resolvedAt)}</span>
                </InfoRow>
              )}
            </div>

            {/* SLA Panel */}
            {(ticket.slaFirstResponseDueAt || ticket.slaResolutionDueAt) && (
              <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm space-y-3">
                <h2 className="text-xs font-semibold text-gray-400 uppercase tracking-wider">
                  SLA Status
                </h2>
                {ticket.slaFirstResponseDueAt && (
                  <div>
                    <p className="text-[11px] text-gray-400 mb-1">First Response Due</p>
                    {ticket.firstRespondedAt
                      ? <span className="flex items-center gap-1.5 text-xs font-medium text-green-600">
                          <CheckCircle2 size={12} /> Responded {formatDate(ticket.firstRespondedAt)}
                        </span>
                      : <SLATimer dueAt={ticket.slaFirstResponseDueAt} label="Due" />
                    }
                  </div>
                )}
                {ticket.slaResolutionDueAt && (
                  <div>
                    <p className="text-[11px] text-gray-400 mb-1">Resolution Due</p>
                    {ticket.resolvedAt
                      ? <span className="flex items-center gap-1.5 text-xs font-medium text-green-600">
                          <CheckCircle2 size={12} /> Resolved {formatDate(ticket.resolvedAt)}
                        </span>
                      : <SLATimer dueAt={ticket.slaResolutionDueAt} label="Due" />
                    }
                  </div>
                )}
                {ticket.isSLABreached && (
                  <div className="flex items-center gap-1.5 text-xs font-semibold text-red-700
                                  bg-red-50 px-3 py-2 rounded-lg border border-red-200">
                    <AlertTriangle size={12} /> SLA Breached
                  </div>
                )}
              </div>
            )}

            {/* Status History Timeline */}
            {history.length > 0 && (
              <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm">
                <h2 className="text-xs font-semibold text-gray-400 uppercase tracking-wider mb-4">
                  Status History
                </h2>
                <ol className="relative border-l border-gray-200 space-y-4 ml-2">
                  {history.map(h => (
                    <li key={h.id} className="ml-4">
                      <div className="absolute -left-1.5 mt-1.5 w-3 h-3 rounded-full
                                      bg-indigo-200 border-2 border-white" />
                      <div>
                        <p className="text-xs text-gray-500">
                          <span className="font-medium text-gray-700">{h.changedByName}</span> changed status
                        </p>
                        <div className="flex items-center gap-1.5 mt-0.5 flex-wrap">
                          <span className={`text-[11px] font-medium px-1.5 py-0.5 rounded ${
                            STATUS_CONFIG[h.fromStatus]?.color ?? 'bg-gray-100 text-gray-500'
                          }`}>
                            {STATUS_CONFIG[h.fromStatus]?.label ?? h.fromStatus}
                          </span>
                          <span className="text-gray-400 text-[10px]">→</span>
                          <span className={`text-[11px] font-medium px-1.5 py-0.5 rounded ${
                            STATUS_CONFIG[h.toStatus]?.color ?? 'bg-gray-100 text-gray-500'
                          }`}>
                            {STATUS_CONFIG[h.toStatus]?.label ?? h.toStatus}
                          </span>
                        </div>
                        {h.note && (
                          <p className="text-[11px] text-gray-500 mt-0.5 italic">"{h.note}"</p>
                        )}
                        <p className="text-[10px] text-gray-400 mt-0.5">{formatDate(h.changedAt)}</p>
                      </div>
                    </li>
                  ))}
                </ol>
              </div>
            )}
          </div>
        </div>
      </div>
    </Layout>
  );
};

export default TicketDetailPage;