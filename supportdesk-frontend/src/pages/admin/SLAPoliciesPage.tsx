/**
 * SLAPoliciesPage — Admin manages SLA policies per ticket priority.
 *
 * CONCEPTS:
 *
 * 1. useQuery for GET /api/sla-policies
 *    Paginated list filtered by active status.
 *    Same pattern as CategoriesPage 
 *
 * 2. useMutation for POST /api/sla-policies
 *    Creates new SLA policy for a priority.
 *    One active policy per priority enforced by backend 
 *
 * 3. useMutation for PUT /api/sla-policies/{id}
 *    Updates name, first response time, resolution time.
 *    Priority cannot be changed after creation 
 *
 * 4. useMutation for PATCH /api/sla-policies/{id}/status
 *    Activates or deactivates a policy.
 *    Deactivated = excluded from new ticket SLA assignments 
 *
 * 5. Minutes → Hours display
 *    Backend stores in minutes (e.g. 60, 240).
 *    UI shows in hours (e.g. 1h, 4h) for readability 
 *
 * 6. Single modal reused for Create and Edit
 *    selectedPolicy = null → Create mode
 *    selectedPolicy = object → Edit mode 
 */

import React, { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import {
  Plus, RefreshCw, X, Clock,
  CheckCircle2, XCircle, Pencil, AlertTriangle,
} from 'lucide-react';
import Layout from '../../components/common/Layout';
import axiosClient from '../../api/axiosClient';
import { ApiResponse } from '../../types/api.types';

// ─────────────────────────────────────────────────────────────────────────────
// TYPES — match SLAPolicyResponse.cs exactly
// ─────────────────────────────────────────────────────────────────────────────

interface SLAPolicyResponse {
  id: string;
  name: string;
  priority: string;       // "Low" | "Medium" | "High" | "Critical"
  firstResponseTimeMinutes: number;
  resolutionTimeMinutes: number;
  isActive: boolean;
  createdAt: string;
}

interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ─────────────────────────────────────────────────────────────────────────────
// ZOD SCHEMA — create / edit SLA policy form
// ─────────────────────────────────────────────────────────────────────────────

const slaPolicySchema = z.object({
  name:                     z.string().min(1, 'Name is required').max(100),
  priority:                 z.number().min(1).max(4),
  firstResponseTimeMinutes: z.number().min(1, 'Must be at least 1 minute'),
  resolutionTimeMinutes:    z.number().min(1, 'Must be at least 1 minute'),
}).refine(
  (data) => data.resolutionTimeMinutes > data.firstResponseTimeMinutes,
  {
    message: 'Resolution time must be greater than first response time',
    path: ['resolutionTimeMinutes'],
  }
);
type SLAPolicyFormData = z.infer<typeof slaPolicySchema>;

// ─────────────────────────────────────────────────────────────────────────────
// CONFIG
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Priority enum values — match backend TicketPriority enum:
 * Low=1, Medium=2, High=3, Critical=4
 */
const PRIORITY_OPTIONS = [
  { label: 'Low',      value: 1, color: 'bg-gray-100 text-gray-600' },
  { label: 'Medium',   value: 2, color: 'bg-blue-100 text-blue-700' },
  { label: 'High',     value: 3, color: 'bg-orange-100 text-orange-700' },
  { label: 'Critical', value: 4, color: 'bg-red-100 text-red-700' },
];

const PRIORITY_CONFIG: Record<string, { color: string; icon: string }> = {
  Low:      { color: 'bg-gray-100 text-gray-600',     icon: '🟢' },
  Medium:   { color: 'bg-blue-100 text-blue-700',     icon: '🔵' },
  High:     { color: 'bg-orange-100 text-orange-700', icon: '🟠' },
  Critical: { color: 'bg-red-100 text-red-700',       icon: '🔴' },
};

// ─────────────────────────────────────────────────────────────────────────────
// HELPERS
// ─────────────────────────────────────────────────────────────────────────────

/**
 * CONCEPT: Minutes → human readable
 * Backend stores 60 → UI shows "1h"
 * Backend stores 90 → UI shows "1h 30m"
 * Backend stores 30 → UI shows "30m" 
 */
function minutesToDisplay(minutes: number): string {
  if (minutes < 60) return `${minutes}m`;
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return m > 0 ? `${h}h ${m}m` : `${h}h`;
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', {
    month: 'short', day: 'numeric', year: 'numeric',
  });
}

// ─────────────────────────────────────────────────────────────────────────────
// SUB-COMPONENT: SLAPolicyModal — Create and Edit
// ─────────────────────────────────────────────────────────────────────────────

function SLAPolicyModal({
  policy,
  onClose,
  onSuccess,
}: {
  policy: SLAPolicyResponse | null;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const isEditMode = policy !== null;

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors },
  } = useForm<SLAPolicyFormData>({
    resolver: zodResolver(slaPolicySchema),
    defaultValues: {
      name:                     '',
      priority:                 1,
      firstResponseTimeMinutes: 60,
      resolutionTimeMinutes:    240,
    },
  });

  // Watch values to show live preview 
  const firstResponse = watch('firstResponseTimeMinutes');
  const resolution    = watch('resolutionTimeMinutes');

  /**
   * Populate form in edit mode 
   */
  useEffect(() => {
    if (policy) {
      reset({
        name:                     policy.name,
        priority:                 PRIORITY_OPTIONS.find(p => p.label === policy.priority)?.value ?? 1,
        firstResponseTimeMinutes: policy.firstResponseTimeMinutes,
        resolutionTimeMinutes:    policy.resolutionTimeMinutes,
      });
    } else {
      reset({
        name: '', priority: 1,
        firstResponseTimeMinutes: 60,
        resolutionTimeMinutes: 240,
      });
    }
  }, [policy, reset]);

  // ─── Mutation: POST /api/sla-policies ───────────────────────────
  const createMutation = useMutation({
    mutationFn: (data: SLAPolicyFormData) =>
      axiosClient.post('/sla-policies', data),
    onSuccess: () => {
      toast.success('SLA policy created');
      onSuccess();
      onClose();
    },
    onError: (error: any) => {
      const message = error.response?.data?.message ||
        'Failed to create SLA policy. A policy for this priority may already exist.';
      toast.error(message);
    },
  });

  // ─── Mutation: PUT /api/sla-policies/{id} ───────────────────────
  const updateMutation = useMutation({
    mutationFn: (data: SLAPolicyFormData) =>
      axiosClient.put(`/sla-policies/${policy?.id}`, {
        name:                     data.name,
        firstResponseTimeMinutes: data.firstResponseTimeMinutes,
        resolutionTimeMinutes:    data.resolutionTimeMinutes,
      }),
    onSuccess: () => {
      toast.success('SLA policy updated');
      onSuccess();
      onClose();
    },
    onError: (error: any) => {
      const message = error.response?.data?.message || 'Failed to update SLA policy';
      toast.error(message);
    },
  });

  const isPending = createMutation.isPending || updateMutation.isPending;

  function onSubmit(data: SLAPolicyFormData) {
    isEditMode ? updateMutation.mutate(data) : createMutation.mutate(data);
  }

  return (
    <div
      className="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4"
      onClick={onClose}
    >
      <div
        className="bg-white rounded-2xl shadow-xl w-full max-w-md p-6"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-center justify-between mb-5">
          <div>
            <h2 className="text-lg font-bold text-gray-900">
              {isEditMode ? 'Edit SLA Policy' : 'New SLA Policy'}
            </h2>
            <p className="text-xs text-gray-400 mt-0.5">
              Define response and resolution time targets
            </p>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 rounded-lg hover:bg-gray-100 text-gray-400"
          >
            <X size={16} />
          </button>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">

          {/* Policy Name */}
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">
              Policy Name <span className="text-red-500">*</span>
            </label>
            <input
              {...register('name')}
              type="text"
              placeholder="e.g. High Priority SLA"
              className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg
                         focus:outline-none focus:ring-2 focus:ring-indigo-300"
            />
            {errors.name && (
              <p className="text-xs text-red-500 mt-1">{errors.name.message}</p>
            )}
          </div>

          {/* Priority — disabled in edit mode */}
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">
              Priority <span className="text-red-500">*</span>
              {isEditMode && (
                <span className="ml-2 text-gray-400 font-normal">
                  (cannot change after creation)
                </span>
              )}
            </label>
            <select
              {...register('priority', { valueAsNumber: true })}
              disabled={isEditMode}
              className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg
                         bg-white focus:outline-none focus:ring-2 focus:ring-indigo-300
                         disabled:bg-gray-50 disabled:cursor-not-allowed"
            >
              {PRIORITY_OPTIONS.map(p => (
                <option key={p.value} value={p.value}>{p.label}</option>
              ))}
            </select>
          </div>

          {/* First Response Time */}
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">
              First Response Time (minutes) <span className="text-red-500">*</span>
            </label>
            <div className="flex items-center gap-2">
              <input
                {...register('firstResponseTimeMinutes', { valueAsNumber: true })}
                type="number"
                min={1}
                placeholder="60"
                className="flex-1 px-3 py-2 text-sm border border-gray-200 rounded-lg
                           focus:outline-none focus:ring-2 focus:ring-indigo-300"
              />
              {/* Live preview */}
              {firstResponse > 0 && (
                <span className="text-xs font-medium text-indigo-600 bg-indigo-50
                                 px-2.5 py-1.5 rounded-lg shrink-0">
                  = {minutesToDisplay(firstResponse)}
                </span>
              )}
            </div>
            {errors.firstResponseTimeMinutes && (
              <p className="text-xs text-red-500 mt-1">
                {errors.firstResponseTimeMinutes.message}
              </p>
            )}
          </div>

          {/* Resolution Time */}
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">
              Resolution Time (minutes) <span className="text-red-500">*</span>
            </label>
            <div className="flex items-center gap-2">
              <input
                {...register('resolutionTimeMinutes', { valueAsNumber: true })}
                type="number"
                min={1}
                placeholder="240"
                className="flex-1 px-3 py-2 text-sm border border-gray-200 rounded-lg
                           focus:outline-none focus:ring-2 focus:ring-indigo-300"
              />
              {resolution > 0 && (
                <span className="text-xs font-medium text-indigo-600 bg-indigo-50
                                 px-2.5 py-1.5 rounded-lg shrink-0">
                  = {minutesToDisplay(resolution)}
                </span>
              )}
            </div>
            {errors.resolutionTimeMinutes && (
              <p className="text-xs text-red-500 mt-1">
                {errors.resolutionTimeMinutes.message}
              </p>
            )}
          </div>

          {/* Info box — common SLA values reference */}
          <div className="bg-gray-50 border border-gray-200 rounded-lg px-3 py-2.5">
            <p className="text-[11px] font-medium text-gray-500 mb-1">
              Common SLA targets:
            </p>
            <div className="grid grid-cols-2 gap-1 text-[11px] text-gray-500">
              <span>🔴 Critical: 1h / 4h</span>
              <span>🟠 High: 4h / 24h</span>
              <span>🔵 Medium: 8h / 48h</span>
              <span>🟢 Low: 24h / 72h</span>
            </div>
          </div>

          {/* Buttons */}
          <div className="flex items-center justify-end gap-3 pt-1">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-sm text-gray-600 border border-gray-200
                         rounded-lg hover:bg-gray-50 transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isPending}
              className="flex items-center gap-2 px-5 py-2 text-sm font-medium
                         bg-indigo-600 text-white rounded-lg hover:bg-indigo-700
                         transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isPending
                ? isEditMode ? 'Saving...' : 'Creating...'
                : isEditMode ? 'Save Changes' : 'Create Policy'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// MAIN PAGE COMPONENT
// ─────────────────────────────────────────────────────────────────────────────

const SLAPoliciesPage: React.FC = () => {
  const queryClient = useQueryClient();

  // ─── Local state ─────────────────────────────────────────────────
  const [activeFilter, setActiveFilter]       = useState<string>('');
  const [showModal, setShowModal]             = useState(false);
  const [selectedPolicy, setSelectedPolicy]   = useState<SLAPolicyResponse | null>(null);

  // ─── Query: GET /api/sla-policies ───────────────────────────────
  const {
    data: policiesData,
    isLoading,
    isError,
    refetch,
  } = useQuery<PagedResult<SLAPolicyResponse>>({
    queryKey: ['sla-policies', activeFilter],
    queryFn: () => {
      const params = new URLSearchParams();
      params.append('page', '1');
      params.append('pageSize', '20');
      if (activeFilter !== '') params.append('isActive', activeFilter);

      return axiosClient
        .get<ApiResponse<PagedResult<SLAPolicyResponse>>>(
          `/sla-policies?${params.toString()}`
        )
        .then((r) => {
          if (!r.data.data) throw new Error('Failed to load SLA policies');
          return r.data.data;
        });
    },
  });

  // ─── Mutation: PATCH /api/sla-policies/{id}/status ───────────────
  const statusMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      axiosClient.patch(`/sla-policies/${id}/status`, { isActive }),
    onSuccess: (_, variables) => {
      toast.success(variables.isActive ? 'Policy activated' : 'Policy deactivated');
      queryClient.invalidateQueries({ queryKey: ['sla-policies'] });
    },
    onError: () => toast.error('Failed to update policy status'),
  });

  // ─── Derived values ──────────────────────────────────────────────
  const policies   = policiesData?.items      ?? [];
  const totalCount = policiesData?.totalCount ?? 0;

  function openCreateModal() {
    setSelectedPolicy(null);
    setShowModal(true);
  }

  function openEditModal(policy: SLAPolicyResponse) {
    setSelectedPolicy(policy);
    setShowModal(true);
  }

  function handleModalSuccess() {
    queryClient.invalidateQueries({ queryKey: ['sla-policies'] });
  }

  // ─────────────────────────────────────────────────────────────────
  // RENDER
  // ─────────────────────────────────────────────────────────────────

  return (
    <Layout>
      <div className="space-y-5">

        {/* ── PAGE HEADER ── */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">SLA Policies</h1>
            <p className="text-sm text-gray-500 mt-1">
              {totalCount} polic{totalCount !== 1 ? 'ies' : 'y'} configured
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
            <button
              onClick={openCreateModal}
              className="flex items-center gap-2 px-4 py-2 text-sm font-medium
                         bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
            >
              <Plus size={15} /> New Policy
            </button>
          </div>
        </div>

        {/* ── INFO BANNER ── */}
        <div className="flex items-start gap-3 bg-amber-50 border border-amber-200
                        rounded-xl px-4 py-3">
          <AlertTriangle size={16} className="text-amber-600 mt-0.5 shrink-0" />
          <div>
            <p className="text-xs font-semibold text-amber-700">How SLA Policies Work</p>
            <p className="text-xs text-amber-600 mt-0.5">
              Each active policy applies to new tickets of that priority.
              One active policy per priority is allowed.
              Deactivating a policy removes it from new ticket assignments —
              existing ticket SLA deadlines are not affected.
            </p>
          </div>
        </div>

        {/* ── FILTER TABS ── */}
        <div className="flex items-center gap-1 bg-gray-100 p-1 rounded-xl w-fit">
          {[
            { label: 'All',      value: '' },
            { label: 'Active',   value: 'true' },
            { label: 'Inactive', value: 'false' },
          ].map((tab) => (
            <button
              key={tab.value}
              onClick={() => setActiveFilter(tab.value)}
              className={`px-4 py-1.5 text-sm font-medium rounded-lg transition-colors ${
                activeFilter === tab.value
                  ? 'bg-white text-gray-800 shadow-sm'
                  : 'text-gray-500 hover:text-gray-700'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {/* ── SLA POLICIES CARDS ── */}
        {isLoading && (
          <div className="flex items-center justify-center h-48">
            <div className="flex flex-col items-center gap-3 text-gray-400">
              <div className="animate-spin rounded-full h-7 w-7 border-b-2 border-indigo-600" />
              <span className="text-sm">Loading SLA policies...</span>
            </div>
          </div>
        )}

        {isError && (
          <div className="flex flex-col items-center justify-center h-48 gap-3 text-gray-500">
            <p className="text-sm">Failed to load SLA policies.</p>
            <button onClick={() => refetch()} className="text-sm text-indigo-600 hover:underline">
              Try again
            </button>
          </div>
        )}

        {!isLoading && !isError && policies.length === 0 && (
          <div className="flex flex-col items-center justify-center h-48 gap-2
                          bg-white border border-gray-200 rounded-xl text-gray-400">
            <Clock size={32} className="opacity-30" />
            <p className="text-sm">No SLA policies yet.</p>
            <button
              onClick={openCreateModal}
              className="text-xs text-indigo-600 hover:underline"
            >
              Create your first SLA policy
            </button>
          </div>
        )}

        {/**
         * CONCEPT: Card layout instead of table
         * SLA policies have few items (max 4 — one per priority).
         * Cards show more info clearly than table rows 
         */}
        {!isLoading && !isError && policies.length > 0 && (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {policies.map((policy) => {
              const priorityCfg = PRIORITY_CONFIG[policy.priority] ?? {
                color: 'bg-gray-100 text-gray-600', icon: '⚪',
              };
              return (
                <div
                  key={policy.id}
                  className={`bg-white border rounded-xl p-5 shadow-sm ${
                    policy.isActive ? 'border-gray-200' : 'border-dashed border-gray-300 opacity-70'
                  }`}
                >
                  {/* Card header */}
                  <div className="flex items-start justify-between mb-4">
                    <div className="flex items-center gap-2">
                      <span className="text-lg">{priorityCfg.icon}</span>
                      <div>
                        <p className="font-semibold text-gray-800">{policy.name}</p>
                        <span className={`inline-flex items-center px-2 py-0.5 rounded-full
                                         text-xs font-semibold mt-0.5 ${priorityCfg.color}`}>
                          {policy.priority}
                        </span>
                      </div>
                    </div>

                    {/* Active / Inactive badge */}
                    {policy.isActive ? (
                      <span className="flex items-center gap-1 text-xs font-semibold
                                       text-green-700 bg-green-100 px-2 py-0.5 rounded-full">
                        <CheckCircle2 size={11} /> Active
                      </span>
                    ) : (
                      <span className="flex items-center gap-1 text-xs font-semibold
                                       text-red-600 bg-red-100 px-2 py-0.5 rounded-full">
                        <XCircle size={11} /> Inactive
                      </span>
                    )}
                  </div>

                  {/* SLA times */}
                  <div className="grid grid-cols-2 gap-3 mb-4">
                    <div className="bg-blue-50 rounded-lg px-3 py-2.5">
                      <p className="text-[10px] text-blue-500 font-medium uppercase tracking-wide">
                        First Response
                      </p>
                      <p className="text-xl font-bold text-blue-700 mt-0.5">
                        {minutesToDisplay(policy.firstResponseTimeMinutes)}
                      </p>
                      <p className="text-[10px] text-blue-400">
                        {policy.firstResponseTimeMinutes} minutes
                      </p>
                    </div>
                    <div className="bg-indigo-50 rounded-lg px-3 py-2.5">
                      <p className="text-[10px] text-indigo-500 font-medium uppercase tracking-wide">
                        Resolution
                      </p>
                      <p className="text-xl font-bold text-indigo-700 mt-0.5">
                        {minutesToDisplay(policy.resolutionTimeMinutes)}
                      </p>
                      <p className="text-[10px] text-indigo-400">
                        {policy.resolutionTimeMinutes} minutes
                      </p>
                    </div>
                  </div>

                  {/* Created date */}
                  <p className="text-[11px] text-gray-400 mb-3">
                    Created {formatDate(policy.createdAt)}
                  </p>

                  {/* Action buttons */}
                  <div className="flex items-center gap-2">
                    <button
                      onClick={() => openEditModal(policy)}
                      className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium
                                 text-gray-600 border border-gray-200 rounded-lg
                                 hover:bg-gray-50 transition-colors"
                    >
                      <Pencil size={11} /> Edit
                    </button>
                    <button
                      onClick={() => statusMutation.mutate({
                        id: policy.id,
                        isActive: !policy.isActive,
                      })}
                      disabled={statusMutation.isPending}
                      className={`px-3 py-1.5 text-xs font-medium rounded-lg border
                                  transition-colors disabled:opacity-50 ${
                        policy.isActive
                          ? 'text-red-600 border-red-200 hover:bg-red-50'
                          : 'text-green-600 border-green-200 hover:bg-green-50'
                      }`}
                    >
                      {policy.isActive ? 'Deactivate' : 'Activate'}
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        )}

      </div>

      {/* ── MODAL ── */}
      {showModal && (
        <SLAPolicyModal
          policy={selectedPolicy}
          onClose={() => setShowModal(false)}
          onSuccess={handleModalSuccess}
        />
      )}

    </Layout>
  );
};

export default SLAPoliciesPage;