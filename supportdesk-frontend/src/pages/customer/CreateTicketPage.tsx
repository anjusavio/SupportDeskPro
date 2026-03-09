/**
 * CreateTicketPage — customer submits a new support ticket.
 *
 * CONCEPTS:
 *
 * 1. useQuery for categories dropdown
 *    Fetches active categories on page load.
 *    Cached by React Query — no refetch if already loaded 
 *
 * 2. React Hook Form + Zod
 *    Same pattern as Login/Register pages.
 *    register() connects input to form 
 *
 * 3. select element — dropdown using categories from API.
 *    Maps array of categories to <option> elements 
 *
 * 4. useMutation (React Query)
 *    For POST/PUT/DELETE — not useQuery (which is for GET).
 *    onSuccess → redirect to my-tickets 
 */
import React from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { ArrowLeft, Send } from 'lucide-react';
import Layout from '../../components/common/Layout';
import { getActiveCategoriesApi } from '../../api/categoryApi';
import { createTicketApi } from '../../api/ticketApi';
// Add to imports
import { useQueryClient } from '@tanstack/react-query';

// Zod validation schema
const createTicketSchema = z.object({
  title: z
    .string()
    .min(1, 'Title is required')
    .max(200, 'Title cannot exceed 200 characters'),
  description: z
    .string()
    .min(1, 'Description is required')
    .max(4000, 'Description cannot exceed 4000 characters'),
  categoryId: z
    .string()
    .min(1, 'Please select a category'),
  priority: z
    .string()
    .min(1, 'Please select a priority'),
});

type CreateTicketFormData = z.infer<typeof createTicketSchema>;

const priorityOptions = [
  { value: '1', label: 'Low',      color: 'text-gray-600' },
  { value: '2', label: 'Medium',   color: 'text-blue-600' },
  { value: '3', label: 'High',     color: 'text-orange-600' },
  { value: '4', label: 'Critical', color: 'text-red-600' },
];

const CreateTicketPage: React.FC = () => {
  const navigate = useNavigate();

  // Fetch active categories for dropdown
  const { data: categoriesData, isLoading: categoriesLoading } = useQuery({
    queryKey: ['activeCategories'],
    queryFn: getActiveCategoriesApi,
  });

  const categories = categoriesData?.data ?? [];

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<CreateTicketFormData>({
    resolver: zodResolver(createTicketSchema),
  });

  /**
   * useMutation — used for POST/PUT/DELETE operations.
   *
   * CONCEPT: useQuery vs useMutation
   * useQuery   → GET requests, auto-fetches, cached 
   * useMutation → POST/PUT/DELETE, triggered manually 
   *
   * onSuccess → runs after successful API call
   * onError   → runs if API call fails
   */

  // Get QueryClient instance to invalidate queries after mutation
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: createTicketApi,
    onSuccess: () => {
      //refresh my-tickets list after creating new ticket
      queryClient.invalidateQueries({ queryKey: ['myTickets'] }); 
      toast.success('Ticket created successfully!');
      navigate('/my-tickets');
    },
    onError: (error: any) => {
      const message =
        error.response?.data?.detail ||
        error.response?.data?.message ||
        'Failed to create ticket. Please try again.';
      toast.error(message);
    },
  });


  const onSubmit = (data: CreateTicketFormData) => {
    mutation.mutate({
      title: data.title,
      description: data.description,
      categoryId: data.categoryId,
      priority: parseInt(data.priority), 
    });
  };

  return (
    <Layout>
      <div className="max-w-2xl mx-auto">

        {/* Header */}
        <div className="flex items-center gap-4 mb-6">
          <button
            onClick={() => navigate('/my-tickets')}
            className="p-2 text-gray-500 hover:bg-gray-100 rounded-lg
                       transition-colors"
          >
            <ArrowLeft size={20} />
          </button>
          <div>
            <h1 className="text-2xl font-bold text-gray-900">
              Create New Ticket
            </h1>
            <p className="text-gray-500 text-sm mt-1">
              Describe your issue and we'll get back to you
            </p>
          </div>
        </div>

        {/* Form Card */}
        <div className="bg-white rounded-xl border border-gray-200 p-6">
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">

            {/* Title */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Title <span className="text-red-500">*</span>
              </label>
              <input
                {...register('title')}
                type="text"
                placeholder="Brief summary of your issue"
                className="w-full px-4 py-2 border border-gray-300 rounded-lg
                           focus:outline-none focus:ring-2 focus:ring-blue-500
                           focus:border-transparent"
              />
              {errors.title && (
                <p className="text-red-500 text-sm mt-1">
                  {errors.title.message}
                </p>
              )}
            </div>

            {/* Category + Priority side by side */}
            <div className="grid grid-cols-2 gap-4">

              {/* Category */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Category <span className="text-red-500">*</span>
                </label>
                <select
                  {...register('categoryId')}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg
                             focus:outline-none focus:ring-2 focus:ring-blue-500
                             focus:border-transparent bg-white"
                >
                  <option value="">
                    {categoriesLoading ? 'Loading...' : 'Select category'}
                  </option>
                  {categories.map((cat) => (
                    <option key={cat.id} value={cat.id}>
                      {cat.parentCategoryName
                        ? `${cat.parentCategoryName} → ${cat.name}`
                        : cat.name}
                    </option>
                  ))}
                </select>
                {errors.categoryId && (
                  <p className="text-red-500 text-sm mt-1">
                    {errors.categoryId.message}
                  </p>
                )}
              </div>

              {/* Priority */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Priority <span className="text-red-500">*</span>
                </label>
                <select
                  {...register('priority')}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg
                             focus:outline-none focus:ring-2 focus:ring-blue-500
                             focus:border-transparent bg-white"
                >
                  <option value="">Select priority</option>
                  {priorityOptions.map((p) => (
                    <option key={p.value} value={p.value}>
                      {p.label}
                    </option>
                  ))}
                </select>
                {errors.priority && (
                  <p className="text-red-500 text-sm mt-1">
                    {errors.priority.message}
                  </p>
                )}
              </div>
            </div>

            {/* Description */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Description <span className="text-red-500">*</span>
              </label>
              <textarea
                {...register('description')}
                rows={6}
                placeholder="Describe your issue in detail..."
                className="w-full px-4 py-2 border border-gray-300 rounded-lg
                           focus:outline-none focus:ring-2 focus:ring-blue-500
                           focus:border-transparent resize-none"
              />
              {errors.description && (
                <p className="text-red-500 text-sm mt-1">
                  {errors.description.message}
                </p>
              )}
            </div>

            {/* Buttons */}
            <div className="flex items-center justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => navigate('/my-tickets')}
                className="px-4 py-2 text-sm text-gray-600 border border-gray-300
                           rounded-lg hover:bg-gray-50 transition-colors"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={mutation.isPending}
                className="flex items-center gap-2 px-6 py-2 bg-blue-600
                           text-white text-sm font-medium rounded-lg
                           hover:bg-blue-700 transition-colors
                           disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <Send size={16} />
                {mutation.isPending ? 'Submitting...' : 'Submit Ticket'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Layout>
  );
};

export default CreateTicketPage;
