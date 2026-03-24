/**
 * CategoriesPage — Admin manages ticket categories.
 *
 * CONCEPTS:
 *
 * 1. useQuery for GET /api/categories
 *    Paginated list with isActive filter.
 *    Same pattern as TicketsPage and UsersPage 
 *
 * 2. useMutation for POST /api/categories
 *    Creates new category with optional parent.
 *    Modal form with React Hook Form + Zod 
 *
 * 3. useMutation for PUT /api/categories/{id}
 *    Edits existing category.
 *    Same modal form reused for create and edit 
 *
 * 4. useMutation for PATCH /api/categories/{id}/status
 *    Toggles active/inactive.
 *    Deactivated = hidden from ticket creation dropdown 
 *
 * 5. Single modal for both Create and Edit
 *    selectedCategory state = null → Create mode
 *    selectedCategory state = category → Edit mode
 *    useEffect resets form when mode changes 
 */

import React, { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import {
  Plus, RefreshCw, X, Tag,
  CheckCircle2, XCircle, Pencil,
} from 'lucide-react';
import Layout from '../../components/common/Layout';
import axiosClient from '../../api/axiosClient';
import { ApiResponse } from '../../types/api.types';

// ─────────────────────────────────────────────────────────────────────────────
// TYPES — match CategoryResponse.cs exactly
// ─────────────────────────────────────────────────────────────────────────────

interface CategoryResponse {
  id: string;
  name: string;
  description: string | null;
  parentCategoryId: string | null;
  parentCategoryName: string | null;
  sortOrder: number;
  isActive: boolean;
  ticketCount: number;
}

interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ─────────────────────────────────────────────────────────────────────────────
// ZOD SCHEMA — create / edit category form
// Matches CreateCategoryRequest and UpdateCategoryRequest 
// ─────────────────────────────────────────────────────────────────────────────

const categorySchema = z.object({
  name:             z.string().min(1, 'Name is required').max(100),
  description:      z.string().max(500).optional(),
  parentCategoryId: z.string().optional(),
  sortOrder:        z.number().min(0).max(9999),
});
type CategoryFormData = z.infer<typeof categorySchema>;

// ─────────────────────────────────────────────────────────────────────────────
// SUB-COMPONENT: CategoryModal — used for both Create and Edit
// ─────────────────────────────────────────────────────────────────────────────

function CategoryModal({
  category,
  parentOptions,
  onClose,
  onSuccess,
}: {
  category: CategoryResponse | null; // null = create mode, object = edit mode
  parentOptions: CategoryResponse[];
  onClose: () => void;
  onSuccess: () => void;
}) {
  const isEditMode = category !== null;

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CategoryFormData>({
    resolver: zodResolver(categorySchema),
    defaultValues: {
      name:             '',
      description:      '',
      parentCategoryId: '',
      sortOrder:        0,
    },
  });

  /**
   * CONCEPT: useEffect to populate form in edit mode
   * When category prop changes → reset form with category values.
   * Without this — form would always show empty fields 
   */
  useEffect(() => {
    if (category) {
      reset({
        name:             category.name,
        description:      category.description ?? '',
        parentCategoryId: category.parentCategoryId ?? '',
        sortOrder:        category.sortOrder,
      });
    } else {
      reset({ name: '', description: '', parentCategoryId: '', sortOrder: 0 });
    }
  }, [category, reset]);

  // ─── Mutation: POST /api/categories ────────────────────────────
  const createMutation = useMutation({
    mutationFn: (data: CategoryFormData) =>
      axiosClient.post('/categories', {
        name:             data.name,
        description:      data.description || null,
        parentCategoryId: data.parentCategoryId || null,
        sortOrder:        data.sortOrder,
      }),
    onSuccess: () => {
      toast.success('Category created successfully');
      onSuccess();
      onClose();
    },
    onError: (error: any) => {
      const message = error.response?.data?.message || 'Failed to create category';
      toast.error(message);
    },
  });

  // ─── Mutation: PUT /api/categories/{id} ────────────────────────
  const updateMutation = useMutation({
    mutationFn: (data: CategoryFormData) =>
      axiosClient.put(`/categories/${category?.id}`, {
        name:             data.name,
        description:      data.description || null,
        parentCategoryId: data.parentCategoryId || null,
        sortOrder:        data.sortOrder,
      }),
    onSuccess: () => {
      toast.success('Category updated successfully');
      onSuccess();
      onClose();
    },
    onError: (error: any) => {
      const message = error.response?.data?.message || 'Failed to update category';
      toast.error(message);
    },
  });

  const isPending = createMutation.isPending || updateMutation.isPending;

  function onSubmit(data: CategoryFormData) {
    if (isEditMode) {
      updateMutation.mutate(data);
    } else {
      createMutation.mutate(data);
    }
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
              {isEditMode ? 'Edit Category' : 'New Category'}
            </h2>
            <p className="text-xs text-gray-400 mt-0.5">
              {isEditMode ? 'Update category details' : 'Add a new ticket category'}
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
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">

          {/* Name */}
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">
              Category Name <span className="text-red-500">*</span>
            </label>
            <input
              {...register('name')}
              type="text"
              placeholder="e.g. Technical, Billing, Network Issues"
              className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg
                         focus:outline-none focus:ring-2 focus:ring-indigo-300"
            />
            {errors.name && (
              <p className="text-xs text-red-500 mt-1">{errors.name.message}</p>
            )}
          </div>

          {/* Description */}
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">
              Description <span className="text-gray-400">(optional)</span>
            </label>
            <textarea
              {...register('description')}
              rows={2}
              placeholder="Brief description of this category"
              className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg
                         resize-none focus:outline-none focus:ring-2 focus:ring-indigo-300"
            />
          </div>

          {/* Parent Category */}
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">
              Parent Category <span className="text-gray-400">(optional)</span>
            </label>
            <select
              {...register('parentCategoryId')}
              className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg
                         bg-white focus:outline-none focus:ring-2 focus:ring-indigo-300"
            >
              <option value="">No parent (top-level)</option>
              {parentOptions
                .filter(p => p.id !== category?.id) // prevent self as parent
                .filter(p => p.parentCategoryId === null) // only top-level as parents
                .map(p => (
                  <option key={p.id} value={p.id}>{p.name}</option>
                ))}
            </select>
          </div>

          {/* Sort Order */}
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">
              Sort Order
            </label>
            <input
              {...register('sortOrder', { valueAsNumber: true })}
              type="number"
              min={0}
              placeholder="0"
              className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg
                         focus:outline-none focus:ring-2 focus:ring-indigo-300"
            />
            <p className="text-[11px] text-gray-400 mt-1">
              Lower number = appears first in dropdown
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
              disabled={isPending}
              className="flex items-center gap-2 px-5 py-2 text-sm font-medium
                         bg-indigo-600 text-white rounded-lg hover:bg-indigo-700
                         transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isPending
                ? isEditMode ? 'Saving...' : 'Creating...'
                : isEditMode ? 'Save Changes' : 'Create Category'}
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

const CategoriesPage: React.FC = () => {
  const queryClient = useQueryClient();

  // ─── Local state ─────────────────────────────────────────────────
  const [page, setPage]                     = useState(1);
  const [activeFilter, setActiveFilter]     = useState<string>('');
  const [showModal, setShowModal]           = useState(false);
  const [selectedCategory, setSelectedCategory] = useState<CategoryResponse | null>(null);

  // ─── Query: GET /api/categories ──────────────────────────────────
  const {
    data: categoriesData,
    isLoading,
    isError,
    refetch,
  } = useQuery<PagedResult<CategoryResponse>>({
    queryKey: ['categories', page, activeFilter],
    queryFn: () => {
      const params = new URLSearchParams();
      params.append('page', String(page));
      params.append('pageSize', '20');
      if (activeFilter !== '') params.append('isActive', activeFilter);

      return axiosClient
        .get<ApiResponse<PagedResult<CategoryResponse>>>(`/categories?${params.toString()}`)
        .then((r) => {
          if (!r.data.data) throw new Error('Failed to load categories');
          return r.data.data;
        });
    },
  });

  // ─── Mutation: PATCH /api/categories/{id}/status ─────────────────
  const statusMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      axiosClient.patch(`/categories/${id}/status`, { isActive }),
    onSuccess: (_, variables) => {
      toast.success(variables.isActive ? 'Category activated' : 'Category deactivated');
      queryClient.invalidateQueries({ queryKey: ['categories'] });
      queryClient.invalidateQueries({ queryKey: ['activeCategories'] }); // refresh dropdown 
    },
    onError: () => toast.error('Failed to update category status'),
  });

  // ─── Derived values ──────────────────────────────────────────────
  const categories = categoriesData?.items      ?? [];
  const totalPages = categoriesData?.totalPages ?? 1;
  const totalCount = categoriesData?.totalCount ?? 0;

  function openCreateModal() {
    setSelectedCategory(null); // null = create mode 
    setShowModal(true);
  }

  function openEditModal(category: CategoryResponse) {
    setSelectedCategory(category); // object = edit mode 
    setShowModal(true);
  }

  function handleModalSuccess() {
    queryClient.invalidateQueries({ queryKey: ['categories'] });
    queryClient.invalidateQueries({ queryKey: ['activeCategories'] }); // refresh ticket dropdown 
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
            <h1 className="text-2xl font-bold text-gray-900">Categories</h1>
            <p className="text-sm text-gray-500 mt-1">
              {totalCount} categor{totalCount !== 1 ? 'ies' : 'y'} configured
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
              <Plus size={15} /> New Category
            </button>
          </div>
        </div>

        {/* ── FILTER BAR ── */}
        <div className="flex items-center gap-1 bg-gray-100 p-1 rounded-xl w-fit">
          {[
            { label: 'All',      value: '' },
            { label: 'Active',   value: 'true' },
            { label: 'Inactive', value: 'false' },
          ].map((tab) => (
            <button
              key={tab.value}
              onClick={() => { setActiveFilter(tab.value); setPage(1); }}
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

        {/* ── CATEGORIES TABLE ── */}
        <div className="bg-white border border-gray-200 rounded-xl shadow-sm overflow-hidden">

          {/* Loading */}
          {isLoading && (
            <div className="flex items-center justify-center h-48">
              <div className="flex flex-col items-center gap-3 text-gray-400">
                <div className="animate-spin rounded-full h-7 w-7 border-b-2 border-indigo-600" />
                <span className="text-sm">Loading categories...</span>
              </div>
            </div>
          )}

          {/* Error */}
          {isError && (
            <div className="flex flex-col items-center justify-center h-48 gap-3 text-gray-500">
              <p className="text-sm">Failed to load categories.</p>
              <button onClick={() => refetch()} className="text-sm text-indigo-600 hover:underline">
                Try again
              </button>
            </div>
          )}

          {/* Empty */}
          {!isLoading && !isError && categories.length === 0 && (
            <div className="flex flex-col items-center justify-center h-48 gap-2 text-gray-400">
              <Tag size={32} className="opacity-30" />
              <p className="text-sm">No categories yet.</p>
              <button
                onClick={openCreateModal}
                className="text-xs text-indigo-600 hover:underline"
              >
                Create your first category
              </button>
            </div>
          )}

          {/* Table */}
          {!isLoading && !isError && categories.length > 0 && (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-100 bg-gray-50 text-left">
                  {['Category', 'Parent', 'Sort Order', 'Tickets', 'Status', 'Actions'].map(h => (
                    <th key={h} className="px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {categories.map((cat) => (
                  <tr key={cat.id} className="hover:bg-gray-50 transition-colors">

                    {/* Name + description */}
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        <div className="h-7 w-7 rounded-lg bg-indigo-100 flex items-center
                                        justify-center shrink-0">
                          <Tag size={13} className="text-indigo-600" />
                        </div>
                        <div>
                          <p className="font-medium text-gray-800">{cat.name}</p>
                          {cat.description && (
                            <p className="text-[11px] text-gray-400 truncate max-w-[200px]">
                              {cat.description}
                            </p>
                          )}
                        </div>
                      </div>
                    </td>

                    {/* Parent category */}
                    <td className="px-4 py-3 text-xs text-gray-500">
                      {cat.parentCategoryName ?? (
                        <span className="text-gray-300 italic">Top level</span>
                      )}
                    </td>

                    {/* Sort order */}
                    <td className="px-4 py-3 text-xs text-gray-500">
                      {cat.sortOrder}
                    </td>

                    {/* Ticket count */}
                    <td className="px-4 py-3">
                      <span className="text-xs font-medium text-gray-700 bg-gray-100
                                       px-2 py-0.5 rounded-full">
                        {cat.ticketCount} tickets
                      </span>
                    </td>

                    {/* Active / Inactive badge */}
                    <td className="px-4 py-3">
                      {cat.isActive ? (
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

                    {/* Actions */}
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        {/* Edit button */}
                        <button
                          onClick={() => openEditModal(cat)}
                          className="flex items-center gap-1 px-2.5 py-1 text-xs font-medium
                                     text-gray-600 border border-gray-200 rounded-lg
                                     hover:bg-gray-50 transition-colors"
                        >
                          <Pencil size={11} /> Edit
                        </button>

                        {/* Activate / Deactivate */}
                        <button
                          onClick={() => statusMutation.mutate({
                            id: cat.id,
                            isActive: !cat.isActive,
                          })}
                          disabled={statusMutation.isPending}
                          className={`px-2.5 py-1 text-xs font-medium rounded-lg border
                                      transition-colors disabled:opacity-50 ${
                            cat.isActive
                              ? 'text-red-600 border-red-200 hover:bg-red-50'
                              : 'text-green-600 border-green-200 hover:bg-green-50'
                          }`}
                        >
                          {cat.isActive ? 'Deactivate' : 'Activate'}
                        </button>
                      </div>
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
              Page {page} of {totalPages} · {totalCount} total
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

      {/* ── MODAL ── */}
      {showModal && (
        <CategoryModal
          category={selectedCategory}
          parentOptions={categories}
          onClose={() => setShowModal(false)}
          onSuccess={handleModalSuccess}
        />
      )}

    </Layout>
  );
};

export default CategoriesPage;