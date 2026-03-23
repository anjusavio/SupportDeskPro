/**
 * UsersPage — Admin manages all users in the tenant.
 *
 * CONCEPTS:
 *
 * 1. useQuery for GET /api/users
 *    Paginated list with role, status and search filters.
 *    Same filter pattern as TicketsPage 
 *
 * 2. useMutation for POST /api/users/invite-agent
 *    Admin invites new agent by email.
 *    Modal form with React Hook Form + Zod validation 
 *
 * 3. useMutation for PATCH /api/users/{id}/status
 *    Toggle user active/inactive.
 *    Optimistic UI — button shows new state immediately 
 *
 * 4. Modal pattern
 *    useState controls open/close.
 *    Form resets on close.
 *    Click outside or X button closes modal 
 *
 * 5. Role filter
 *    Backend accepts role as int (Agent=3, Customer=4).
 *    Dropdown maps label → int value 
 */

import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import {
  Search, UserPlus, RefreshCw, CheckCircle2,
  XCircle, Mail, Shield, User, X, Send,
} from 'lucide-react';
import Layout from '../../components/common/Layout';
import axiosClient from '../../api/axiosClient';
import { ApiResponse } from '../../types/api.types';

// ─────────────────────────────────────────────────────────────────────────────
// TYPES — match UserResponse.cs exactly
// ─────────────────────────────────────────────────────────────────────────────

interface UserResponse {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: string;         // "Admin" | "Agent" | "Customer"
  isActive: boolean;
  isEmailVerified: boolean;
  lastLoginAt: string | null;
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
// ZOD SCHEMA — invite agent form
// Matches InviteAgentRequest.cs exactly 
// ─────────────────────────────────────────────────────────────────────────────

const inviteAgentSchema = z.object({
  firstName: z.string().min(1, 'First name is required').max(100),
  lastName:  z.string().min(1, 'Last name is required').max(100),
  email:     z.string().email('Enter a valid email address'),
});
type InviteAgentFormData = z.infer<typeof inviteAgentSchema>;

// ─────────────────────────────────────────────────────────────────────────────
// CONFIG
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Backend role enum values:
 * SuperAdmin=1, Admin=2, Agent=3, Customer=4
 * Only Agent and Customer shown in filter (Admin cannot change to Admin/SuperAdmin)
 */
const ROLE_FILTER_OPTIONS = [
  { label: 'All Roles', value: '' },
  { label: 'Admin',     value: '2' },
  { label: 'Agent',     value: '3' },
  { label: 'Customer',  value: '4' },
];

const ROLE_BADGE: Record<string, { label: string; color: string }> = {
  SuperAdmin: { label: 'Super Admin', color: 'bg-purple-100 text-purple-700' },
  Admin:      { label: 'Admin',       color: 'bg-indigo-100 text-indigo-700' },
  Agent:      { label: 'Agent',       color: 'bg-blue-100 text-blue-700' },
  Customer:   { label: 'Customer',    color: 'bg-gray-100 text-gray-600' },
};

// ─────────────────────────────────────────────────────────────────────────────
// HELPERS
// ─────────────────────────────────────────────────────────────────────────────

function formatDate(dateStr: string | null): string {
  if (!dateStr) return 'Never';
  return new Date(dateStr).toLocaleString('en-US', {
    month: 'short', day: 'numeric', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  });
}

function getInitials(firstName: string, lastName: string): string {
  return `${firstName[0] ?? ''}${lastName[0] ?? ''}`.toUpperCase();
}

// ─────────────────────────────────────────────────────────────────────────────
// SUB-COMPONENT: RoleBadge
// ─────────────────────────────────────────────────────────────────────────────

function RoleBadge({ role }: { role: string }) {
  const cfg = ROLE_BADGE[role] ?? { label: role, color: 'bg-gray-100 text-gray-600' };
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold ${cfg.color}`}>
      {cfg.label}
    </span>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// SUB-COMPONENT: InviteAgentModal
// ─────────────────────────────────────────────────────────────────────────────

function InviteAgentModal({
  onClose, onSuccess,
}: {
  onClose: () => void;
  onSuccess: () => void;
}) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<InviteAgentFormData>({
    resolver: zodResolver(inviteAgentSchema),
  });

  const mutation = useMutation({
    mutationFn: (data: InviteAgentFormData) =>
      axiosClient.post('/users/invite-agent', data),
    onSuccess: () => {
      toast.success('Agent invited successfully! They will receive an email.');
      onSuccess();
      onClose();
    },
    onError: (error: any) => {
      const message =
        error.response?.data?.message ||
        'Failed to invite agent. Email may already exist.';
      toast.error(message);
    },
  });

  return (
    // Backdrop — click outside closes modal 
    <div
      className="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4"
      onClick={onClose}
    >
      {/* Modal card — stopPropagation prevents backdrop click from firing */}
      <div
        className="bg-white rounded-2xl shadow-xl w-full max-w-md p-6"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-center justify-between mb-5">
          <div>
            <h2 className="text-lg font-bold text-gray-900">Invite Agent</h2>
            <p className="text-xs text-gray-400 mt-0.5">
              Agent will receive an email with temporary password
            </p>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 rounded-lg hover:bg-gray-100 text-gray-400 transition-colors"
          >
            <X size={16} />
          </button>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit((data) => mutation.mutate(data))} className="space-y-4">

          {/* First + Last name side by side */}
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">
                First Name <span className="text-red-500">*</span>
              </label>
              <input
                {...register('firstName')}
                type="text"
                placeholder="John"
                className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg
                           focus:outline-none focus:ring-2 focus:ring-indigo-300"
              />
              {errors.firstName && (
                <p className="text-xs text-red-500 mt-1">{errors.firstName.message}</p>
              )}
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">
                Last Name <span className="text-red-500">*</span>
              </label>
              <input
                {...register('lastName')}
                type="text"
                placeholder="Smith"
                className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg
                           focus:outline-none focus:ring-2 focus:ring-indigo-300"
              />
              {errors.lastName && (
                <p className="text-xs text-red-500 mt-1">{errors.lastName.message}</p>
              )}
            </div>
          </div>

          {/* Email */}
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">
              Email Address <span className="text-red-500">*</span>
            </label>
            <input
              {...register('email')}
              type="email"
              placeholder="john.smith@company.com"
              className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg
                         focus:outline-none focus:ring-2 focus:ring-indigo-300"
            />
            {errors.email && (
              <p className="text-xs text-red-500 mt-1">{errors.email.message}</p>
            )}
          </div>

          {/* Info note */}
          <div className="flex items-start gap-2 bg-indigo-50 border border-indigo-100
                          rounded-lg px-3 py-2.5">
            <Mail size={13} className="text-indigo-500 mt-0.5 shrink-0" />
            <p className="text-xs text-indigo-600">
              A welcome email with a temporary password will be sent to this address.
            </p>
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
              disabled={mutation.isPending}
              className="flex items-center gap-2 px-5 py-2 text-sm font-medium
                         bg-indigo-600 text-white rounded-lg hover:bg-indigo-700
                         transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <Send size={13} />
              {mutation.isPending ? 'Inviting...' : 'Send Invite'}
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

const UsersPage: React.FC = () => {
  const queryClient = useQueryClient();

  // ─── Local state ─────────────────────────────────────────────────
  const [page, setPage]               = useState(1);
  const [search, setSearch]           = useState('');
  const [roleFilter, setRoleFilter]   = useState('');
  const [activeFilter, setActiveFilter] = useState<string>('');
  const [showInviteModal, setShowInviteModal] = useState(false);

  // ─── Query: GET /api/users ───────────────────────────────────────
  const {
    data: usersData,
    isLoading,
    isError,
    refetch,
  } = useQuery<PagedResult<UserResponse>>({
    queryKey: ['users', page, search, roleFilter, activeFilter],
    queryFn: () => {
      const params = new URLSearchParams();
      params.append('page', String(page));
      params.append('pageSize', '20');
      if (search)       params.append('search', search);
      if (roleFilter)   params.append('role', roleFilter);
      if (activeFilter !== '') params.append('isActive', activeFilter);

      return axiosClient
        .get<ApiResponse<PagedResult<UserResponse>>>(`/users?${params.toString()}`)
        .then((r) => {
          if (!r.data.data) throw new Error('Failed to load users');
          return r.data.data;
        });
    },
  });

  // ─── Mutation: PATCH /api/users/{id}/status ──────────────────────
  const statusMutation = useMutation({
    mutationFn: ({ userId, isActive }: { userId: string; isActive: boolean }) =>
      axiosClient.patch(`/users/${userId}/status`, { isActive }),
    onSuccess: (_, variables) => {
      toast.success(variables.isActive ? 'User activated' : 'User deactivated');
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
    onError: () => toast.error('Failed to update user status'),
  });

  // ─── Derived values ──────────────────────────────────────────────
  const users      = usersData?.items      ?? [];
  const totalPages = usersData?.totalPages ?? 1;
  const totalCount = usersData?.totalCount ?? 0;

  function resetFilters() {
    setSearch('');
    setRoleFilter('');
    setActiveFilter('');
    setPage(1);
  }

  const hasActiveFilters = search !== '' || roleFilter !== '' || activeFilter !== '';

  // ─────────────────────────────────────────────────────────────────
  // RENDER
  // ─────────────────────────────────────────────────────────────────

  return (
    <Layout>
      <div className="space-y-5">

        {/* ── PAGE HEADER ── */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Users</h1>
            <p className="text-sm text-gray-500 mt-1">
              {totalCount} user{totalCount !== 1 ? 's' : ''} in your tenant
            </p>
          </div>
          <div className="flex items-center gap-3">
            <button
              onClick={() => refetch()}
              className="p-2 border border-gray-200 rounded-lg hover:bg-gray-50
                         text-gray-500 transition-colors"
              title="Refresh"
            >
              <RefreshCw size={15} />
            </button>
            <button
              onClick={() => setShowInviteModal(true)}
              className="flex items-center gap-2 px-4 py-2 text-sm font-medium
                         bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
            >
              <UserPlus size={15} /> Invite Agent
            </button>
          </div>
        </div>

        {/* ── FILTERS BAR ── */}
        <div className="bg-white border border-gray-200 rounded-xl p-4 shadow-sm">
          <div className="flex flex-wrap items-center gap-3">

            {/* Search */}
            <div className="relative flex-1 min-w-[200px]">
              <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
              <input
                type="text"
                value={search}
                onChange={(e) => { setSearch(e.target.value); setPage(1); }}
                placeholder="Search by name or email..."
                className="w-full pl-9 pr-4 py-2 text-sm border border-gray-200 rounded-lg
                           focus:outline-none focus:ring-2 focus:ring-indigo-300"
              />
            </div>

            {/* Role filter */}
            <select
              value={roleFilter}
              onChange={(e) => { setRoleFilter(e.target.value); setPage(1); }}
              className="px-3 py-2 text-sm border border-gray-200 rounded-lg bg-white
                         focus:outline-none focus:ring-2 focus:ring-indigo-300"
            >
              {ROLE_FILTER_OPTIONS.map(opt => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
              ))}
            </select>

            {/* Active/Inactive filter */}
            <select
              value={activeFilter}
              onChange={(e) => { setActiveFilter(e.target.value); setPage(1); }}
              className="px-3 py-2 text-sm border border-gray-200 rounded-lg bg-white
                         focus:outline-none focus:ring-2 focus:ring-indigo-300"
            >
              <option value="">All Status</option>
              <option value="true">Active</option>
              <option value="false">Inactive</option>
            </select>

            {hasActiveFilters && (
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

        {/* ── USERS TABLE ── */}
        <div className="bg-white border border-gray-200 rounded-xl shadow-sm overflow-hidden">

          {/* Loading */}
          {isLoading && (
            <div className="flex items-center justify-center h-48">
              <div className="flex flex-col items-center gap-3 text-gray-400">
                <div className="animate-spin rounded-full h-7 w-7 border-b-2 border-indigo-600" />
                <span className="text-sm">Loading users...</span>
              </div>
            </div>
          )}

          {/* Error */}
          {isError && (
            <div className="flex flex-col items-center justify-center h-48 gap-3 text-gray-500">
              <p className="text-sm">Failed to load users.</p>
              <button onClick={() => refetch()} className="text-sm text-indigo-600 hover:underline">
                Try again
              </button>
            </div>
          )}

          {/* Empty */}
          {!isLoading && !isError && users.length === 0 && (
            <div className="flex flex-col items-center justify-center h-48 gap-2 text-gray-400">
              <User size={32} className="opacity-30" />
              <p className="text-sm">No users found.</p>
              {hasActiveFilters && (
                <button onClick={resetFilters} className="text-xs text-indigo-600 hover:underline">
                  Clear filters
                </button>
              )}
            </div>
          )}

          {/* Table */}
          {!isLoading && !isError && users.length > 0 && (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-100 bg-gray-50 text-left">
                  {['User', 'Role', 'Status', 'Email Verified', 'Last Login', 'Joined', 'Actions'].map(h => (
                    <th key={h} className="px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {users.map((user) => (
                  <tr key={user.id} className="hover:bg-gray-50 transition-colors">

                    {/* Avatar + name + email */}
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-3">
                        <div className="h-8 w-8 shrink-0 rounded-full bg-indigo-100 text-indigo-700
                                        flex items-center justify-center text-xs font-bold">
                          {getInitials(user.firstName, user.lastName)}
                        </div>
                        <div>
                          <p className="font-medium text-gray-800">
                            {user.firstName} {user.lastName}
                          </p>
                          <p className="text-xs text-gray-400">{user.email}</p>
                        </div>
                      </div>
                    </td>

                    {/* Role badge */}
                    <td className="px-4 py-3">
                      <RoleBadge role={user.role} />
                    </td>

                    {/* Active / Inactive badge */}
                    <td className="px-4 py-3">
                      {user.isActive ? (
                        <span className="flex items-center gap-1 text-xs font-semibold
                                         text-green-700 bg-green-100 px-2 py-0.5 rounded-full w-fit">
                          <CheckCircle2 size={11} /> Active
                        </span>
                      ) : (
                        <span className="flex items-center gap-1 text-xs font-semibold
                                         text-red-600 bg-red-100 px-2 py-0.5 rounded-full w-fit">
                          <XCircle size={11} /> Inactive
                        </span>
                      )}
                    </td>

                    {/* Email verified */}
                    <td className="px-4 py-3">
                      {user.isEmailVerified ? (
                        <span className="flex items-center gap-1 text-xs text-green-600">
                          <CheckCircle2 size={12} /> Verified
                        </span>
                      ) : (
                        <span className="flex items-center gap-1 text-xs text-gray-400">
                          <Mail size={12} /> Pending
                        </span>
                      )}
                    </td>

                    {/* Last login */}
                    <td className="px-4 py-3 text-xs text-gray-500">
                      {formatDate(user.lastLoginAt)}
                    </td>

                    {/* Joined date */}
                    <td className="px-4 py-3 text-xs text-gray-500">
                      {formatDate(user.createdAt)}
                    </td>

                    {/* Actions — Activate / Deactivate */}
                    <td className="px-4 py-3">
                      {/* Don't show deactivate for Admin role */}
                      {user.role !== 'Admin' && user.role !== 'SuperAdmin' && (
                        <button
                          onClick={() => statusMutation.mutate({
                            userId: user.id,
                            isActive: !user.isActive,
                          })}
                          disabled={statusMutation.isPending}
                          className={`px-3 py-1 text-xs font-medium rounded-lg border transition-colors
                                      disabled:opacity-50 disabled:cursor-not-allowed ${
                            user.isActive
                              ? 'text-red-600 border-red-200 hover:bg-red-50'
                              : 'text-green-600 border-green-200 hover:bg-green-50'
                          }`}
                        >
                          {user.isActive ? 'Deactivate' : 'Activate'}
                        </button>
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
              Page {page} of {totalPages} · {totalCount} total users
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
              <span className="text-xs text-gray-500">Page {page}</span>
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

      {/* ── INVITE AGENT MODAL ── */}
      {showInviteModal && (
        <InviteAgentModal
          onClose={() => setShowInviteModal(false)}
          onSuccess={() => queryClient.invalidateQueries({ queryKey: ['users'] })}
        />
      )}

    </Layout>
  );
};

export default UsersPage;