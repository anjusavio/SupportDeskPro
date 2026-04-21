/**
 * TenantsPage — SuperAdmin only. Manages all tenant workspaces on the platform.
 *
 * CONCEPTS:
 *
 * 1. SuperAdmin scope
 *    No tenant isolation here — SuperAdmin sees ALL tenants across the platform.
 *    Every other page is scoped to the current tenant via JWT claims.
 *    This page is the exception — it manages the platform itself.
 *
 * 2. Create + Edit in one modal
 *    Same modal form handles both create and edit.
 *    editingTenant state determines which mode is active.
 *    null = create mode, Tenant object = edit mode.
 *
 * 3. Slug is readonly on edit
 *    Slug identifies the tenant during customer registration.
 *    Changing it after creation would break existing registration links.
 *    Input field is set to readOnly when editing an existing tenant.
 *
 * 4. Soft delete with inline confirmation
 *    DELETE endpoint soft deletes — sets IsDeleted flag, data is retained.
 *    Tenant is deactivated immediately so all users lose access at once.
 *    Inline confirmation banner replaces window.confirm.
 *
 * 5. Active toggle on edit only
 *    New tenants are always created as active.
 *    Active/inactive toggle only appears when editing an existing tenant.
 *    Deactivating blocks all logins for that company without deleting data.
 *
 * 6. Plan limits
 *    MaxAgents and MaxTickets define the plan boundaries per tenant.
 *    SuperAdmin sets these during onboarding and can update them anytime.
 *    Displayed in the table as "10 agents · 1,000 tickets".
 */

import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import {
  Plus, Search, Building2, CheckCircle2,
  XCircle, Pencil, Trash2, X
} from 'lucide-react';
import Layout from '../../components/common/Layout';
import axiosClient from '../../api/axiosClient';
import { ApiResponse } from '../../types/api.types';
export type { };
// ── Types ─────────────────────────────────────────────────────────
interface Tenant {
  id: string;
  name: string;
  slug: string;
  planType: string;
  isActive: boolean;
  maxAgents: number;
  maxTickets: number;
  createdAt: string;
}

// ── Validation schema ─────────────────────────────────────────────
const tenantSchema = z.object({
  name:       z.string().min(1, 'Name is required').max(100),
  slug:       z.string().min(1, 'Slug is required').max(50)
              .regex(/^[a-z0-9-]+$/, 'Lowercase letters, numbers and hyphens only'),
  planType:   z.string().min(1, 'Plan type is required'),
  isActive:   z.boolean(),
  maxAgents:  z.number().min(1).max(1000),
  maxTickets: z.number().min(1).max(100000),
});

type TenantFormData = z.infer<typeof tenantSchema>;

const PLAN_TYPES = [
  { value: 'Free', label: 'Free' },
  { value: 'Pro', label: 'Pro' },
  { value: 'Enterprise', label: 'Enterprise' },
];

// ── Main Component ────────────────────────────────────────────────
const TenantsPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [editingTenant, setEditingTenant] = useState<Tenant | null>(null);

  // ── Delete confirmation state ─────────────────────────────────
  const [deletingTenantId, setDeletingTenantId] = useState<string | null>(null);
  const [deletingTenantName, setDeletingTenantName] = useState<string>('');

  // ── Query: get all tenants ────────────────────────────────────
  const { data, isLoading } = useQuery({
    queryKey: ['tenants', search],
    queryFn: () =>
      axiosClient
        .get<ApiResponse<{ items: Tenant[]; totalCount: number }>>(
          '/tenants', { params: { search, pageSize: 50 } })
        .then(r => r.data.data),
  });

  const tenants = data?.items ?? [];

  // ── Form ──────────────────────────────────────────────────────
  const {
    register,
    handleSubmit,
    reset,
    setValue,
    formState: { errors },
  } = useForm<TenantFormData>({
    resolver: zodResolver(tenantSchema),
    defaultValues: {
      isActive:   true,
      maxAgents:  10,
      maxTickets: 1000,
    },
  });

  // ── Mutation: create ──────────────────────────────────────────
  const createMutation = useMutation({
    mutationFn: (data: TenantFormData) =>
      axiosClient.post('/tenants', {
        ...data,
        planType: data.planType,
      }),
    onSuccess: () => {
      toast.success('Tenant created successfully');
      queryClient.invalidateQueries({ queryKey: ['tenants'] });
      setShowModal(false);
      reset();
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.detail || 'Failed to create tenant');
    },
  });

  // ── Mutation: update ──────────────────────────────────────────
  const updateMutation = useMutation({
    mutationFn: (data: TenantFormData & { id: string }) =>
      axiosClient.put(`/tenants/${data.id}`, {
        name:       data.name,
        planType:   data.planType,
        isActive:   data.isActive,
        maxAgents:  data.maxAgents,
        maxTickets: data.maxTickets,
      }),
    onSuccess: () => {
      toast.success('Tenant updated successfully');
      queryClient.invalidateQueries({ queryKey: ['tenants'] });
      setShowModal(false);
      setEditingTenant(null);
      reset();
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.detail || 'Failed to update tenant');
    },
  });

  // ── Mutation: delete ──────────────────────────────────────────
  const deleteMutation = useMutation({
    mutationFn: (id: string) => axiosClient.delete(`/tenants/${id}`),
    onSuccess: () => {
      toast.success('Tenant deleted successfully');
      queryClient.invalidateQueries({ queryKey: ['tenants'] });
      setDeletingTenantId(null);
      setDeletingTenantName('');
    },
    onError: () => toast.error('Failed to delete tenant'),
  });

  // ── Handlers ──────────────────────────────────────────────────
  function openCreate() {
    setEditingTenant(null);
    reset({ isActive: true, maxAgents: 10, maxTickets: 1000 });
    setShowModal(true);
  }

  function openEdit(tenant: Tenant) {
    setEditingTenant(tenant);
    setValue('name',       tenant.name);
    setValue('slug',       tenant.slug);
    setValue('planType',   tenant.planType);
    setValue('isActive',   tenant.isActive);
    setValue('maxAgents',  tenant.maxAgents);
    setValue('maxTickets', tenant.maxTickets);
    setShowModal(true);
  }

  function onSubmit(data: TenantFormData) {
    if (editingTenant) {
      updateMutation.mutate({ ...data, id: editingTenant.id });
    } else {
      createMutation.mutate(data);
    }
  }

  // ── Render ────────────────────────────────────────────────────
  return (
    <Layout>
      <div className="max-w-6xl mx-auto space-y-6">

        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Tenants</h1>
            <p className="text-sm text-gray-500 mt-1">
              Manage all company workspaces on the platform
            </p>
          </div>
          <button
            onClick={openCreate}
            className="flex items-center gap-2 px-4 py-2 bg-indigo-600
                       text-white text-sm font-medium rounded-xl
                       hover:bg-indigo-700 transition-colors"
          >
            <Plus size={16} /> New Tenant
          </button>
        </div>

        {/* Search */}
        <div className="relative">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2
                                        text-gray-400" />
          <input
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder="Search by name or slug..."
            className="w-full pl-9 pr-4 py-2 text-sm border border-gray-200
                       rounded-xl focus:outline-none focus:ring-2
                       focus:ring-indigo-300 bg-white"
          />
        </div>

        {/* ── Delete confirmation banner — above table ✅ */}
        {deletingTenantId && (
          <div className="flex items-center justify-between gap-4 px-5 py-4
                          bg-red-50 border border-red-200 rounded-xl">
            <p className="text-sm text-red-700">
              Are you sure you want to delete{' '}
              <strong>{deletingTenantName}</strong>?
              This will deactivate all users immediately.
            </p>
            <div className="flex items-center gap-2 shrink-0">
              <button
                onClick={() => {
                  setDeletingTenantId(null);
                  setDeletingTenantName('');
                }}
                className="px-3 py-1.5 text-xs text-gray-600 border
                           border-gray-200 rounded-lg hover:bg-white
                           transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={() => deleteMutation.mutate(deletingTenantId)}
                disabled={deleteMutation.isPending}
                className="px-3 py-1.5 text-xs font-medium bg-red-600
                           text-white rounded-lg hover:bg-red-700
                           transition-colors disabled:opacity-50"
              >
                {deleteMutation.isPending ? 'Deleting...' : 'Yes, Delete'}
              </button>
            </div>
          </div>
        )}

        {/* Tenants table */}
        <div className="bg-white border border-gray-200 rounded-xl
                        shadow-sm overflow-hidden">
          {isLoading ? (
            <div className="flex justify-center py-12">
              <div className="animate-spin rounded-full h-8 w-8
                              border-b-2 border-indigo-600" />
            </div>
          ) : tenants.length === 0 ? (
            <div className="text-center py-12 text-gray-400">
              <Building2 size={32} className="mx-auto mb-2 opacity-40" />
              <p className="text-sm">No tenants found</p>
            </div>
          ) : (
            <table className="w-full">
              <thead className="bg-gray-50 border-b border-gray-200">
                <tr>
                  {['Name', 'Slug', 'Plan', 'Limits', 'Status', 'Created', ''].map(h => (
                    <th key={h} className="px-5 py-3 text-left text-xs
                                           font-semibold text-gray-500 uppercase">
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {tenants.map(tenant => (
                  <tr key={tenant.id}
                    className="hover:bg-gray-50 transition-colors">
                    <td className="px-5 py-4">
                      <div className="flex items-center gap-2">
                        <div className="h-8 w-8 rounded-lg bg-indigo-100
                                        flex items-center justify-center">
                          <Building2 size={14} className="text-indigo-600" />
                        </div>
                        <span className="text-sm font-medium text-gray-900">
                          {tenant.name}
                        </span>
                      </div>
                    </td>
                    <td className="px-5 py-4">
                      <span className="text-xs font-mono bg-gray-100
                                       text-gray-600 px-2 py-1 rounded">
                        {tenant.slug}
                      </span>
                    </td>
                    <td className="px-5 py-4">
                      <span className="text-xs font-medium text-gray-600">
                        {PLAN_TYPES.find(
                          p => p.value === String(tenant.planType))?.label
                          ?? 'Unknown'}
                      </span>
                    </td>
                    <td className="px-5 py-4">
                      <span className="text-xs text-gray-500">
                        {tenant.maxAgents} agents ·{' '}
                        {tenant.maxTickets.toLocaleString()} tickets
                      </span>
                    </td>
                    <td className="px-5 py-4">
                      {tenant.isActive ? (
                        <span className="flex items-center gap-1 text-xs
                                         font-medium text-green-700">
                          <CheckCircle2 size={12} /> Active
                        </span>
                      ) : (
                        <span className="flex items-center gap-1 text-xs
                                         font-medium text-red-600">
                          <XCircle size={12} /> Inactive
                        </span>
                      )}
                    </td>
                    <td className="px-5 py-4">
                      <span className="text-xs text-gray-400">
                        {new Date(tenant.createdAt).toLocaleDateString()}
                      </span>
                    </td>
                    <td className="px-5 py-4">
                      <div className="flex items-center gap-2 justify-end">

                        {/* Edit button */}
                        <button
                          onClick={() => openEdit(tenant)}
                          className="p-1.5 rounded-lg hover:bg-gray-100
                                     text-gray-500 transition-colors"
                        >
                          <Pencil size={14} />
                        </button>

                        {/* Delete button — sets state to show banner */}
                        <button
                          onClick={() => {
                            setDeletingTenantId(tenant.id);
                            setDeletingTenantName(tenant.name);
                          }}
                          className="p-1.5 rounded-lg hover:bg-red-50
                                     text-red-500 transition-colors"
                        >
                          <Trash2 size={14} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>

      {/* ── Create / Edit Modal ───────────────────────────────── */}
      {showModal && (
        <div className="fixed inset-0 bg-black/40 flex items-center
                        justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md">

            {/* Modal header */}
            <div className="flex items-center justify-between px-6 py-4
                            border-b border-gray-100">
              <h2 className="text-sm font-semibold text-gray-900">
                {editingTenant ? 'Edit Tenant' : 'Create New Tenant'}
              </h2>
              <button
                onClick={() => {
                  setShowModal(false);
                  setEditingTenant(null);
                }}
                className="p-1.5 rounded-lg hover:bg-gray-100 text-gray-500"
              >
                <X size={16} />
              </button>
            </div>

            {/* Modal form */}
            <form onSubmit={handleSubmit(onSubmit)} className="p-6 space-y-4">

              {/* Name */}
              <div>
                <label className="block text-xs font-medium text-gray-700 mb-1">
                  Company Name <span className="text-red-500">*</span>
                </label>
                <input
                  {...register('name')}
                  placeholder="Acme Corporation"
                  className="w-full px-3 py-2 text-sm border border-gray-200
                             rounded-lg focus:outline-none focus:ring-2
                             focus:ring-indigo-300"
                />
                {errors.name && (
                  <p className="text-xs text-red-500 mt-1">
                    {errors.name.message}
                  </p>
                )}
              </div>

              {/* Slug — readonly on edit */}
              <div>
                <label className="block text-xs font-medium text-gray-700 mb-1">
                  Slug <span className="text-red-500">*</span>
                  {editingTenant && (
                    <span className="text-gray-400 font-normal ml-1">
                      (cannot be changed)
                    </span>
                  )}
                </label>
                <input
                  {...register('slug')}
                  placeholder="acmecorp"
                  readOnly={!!editingTenant}
                  className={`w-full px-3 py-2 text-sm border border-gray-200
                             rounded-lg focus:outline-none focus:ring-2
                             focus:ring-indigo-300 font-mono
                             ${editingTenant
                               ? 'bg-gray-50 text-gray-400 cursor-not-allowed'
                               : ''}`}
                />
                {errors.slug && (
                  <p className="text-xs text-red-500 mt-1">
                    {errors.slug.message}
                  </p>
                )}
              </div>

              {/* Plan Type */}
              <div>
                <label className="block text-xs font-medium text-gray-700 mb-1">
                  Plan <span className="text-red-500">*</span>
                </label>
                <select
                  {...register('planType')}
                  className="w-full px-3 py-2 text-sm border border-gray-200
                             rounded-lg focus:outline-none focus:ring-2
                             focus:ring-indigo-300 bg-white"
                >
                  <option value="">Select plan</option>
                  {PLAN_TYPES.map(p => (
                    <option key={p.value} value={p.value}>{p.label}</option>
                  ))}
                </select>
                {errors.planType && (
                  <p className="text-xs text-red-500 mt-1">
                    {errors.planType.message}
                  </p>
                )}
              </div>

              {/* Max Agents + Max Tickets side by side */}
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-medium text-gray-700 mb-1">
                    Max Agents
                  </label>
                  <input
                    {...register('maxAgents', { valueAsNumber: true })}
                    type="number"
                    min={1}
                    className="w-full px-3 py-2 text-sm border border-gray-200
                               rounded-lg focus:outline-none focus:ring-2
                               focus:ring-indigo-300"
                  />
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-700 mb-1">
                    Max Tickets
                  </label>
                  <input
                    {...register('maxTickets', { valueAsNumber: true })}
                    type="number"
                    min={1}
                    className="w-full px-3 py-2 text-sm border border-gray-200
                               rounded-lg focus:outline-none focus:ring-2
                               focus:ring-indigo-300"
                  />
                </div>
              </div>

              {/* Active toggle — only on edit */}
              {editingTenant && (
                <div className="flex items-center gap-2">
                  <input
                    {...register('isActive')}
                    type="checkbox"
                    className="rounded"
                  />
                  <label className="text-xs text-gray-700">
                    Active — uncheck to deactivate all logins for this tenant
                  </label>
                </div>
              )}

              {/* Buttons */}
              <div className="flex gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => {
                    setShowModal(false);
                    setEditingTenant(null);
                  }}
                  className="flex-1 px-4 py-2 text-sm text-gray-600
                             border border-gray-200 rounded-xl
                             hover:bg-gray-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={
                    createMutation.isPending || updateMutation.isPending
                  }
                  className="flex-1 px-4 py-2 text-sm font-medium
                             bg-indigo-600 text-white rounded-xl
                             hover:bg-indigo-700 disabled:opacity-50"
                >
                  {createMutation.isPending || updateMutation.isPending
                    ? 'Saving...'
                    : editingTenant ? 'Save Changes' : 'Create Tenant'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </Layout>
  );
};

export default TenantsPage;