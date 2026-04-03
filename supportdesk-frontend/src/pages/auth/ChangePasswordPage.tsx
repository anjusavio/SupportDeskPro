/**
 * ChangePasswordPage — allows logged-in users to change their password.
 *
 * CONCEPTS:
 * 1. Requires current password — security check 
 * 2. Zod refine — new password must match confirm 
 * 3. On success → logout and redirect to login
 *    (refresh tokens invalidated on backend) 
 */

import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation } from '@tanstack/react-query';
import { useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { Lock, Eye, EyeOff } from 'lucide-react';
import Layout from '../../components/common/Layout';
import axiosClient from '../../api/axiosClient';
import useAuthStore from '../../store/authStore';

// ── Schema ──────────────────────────────────────────────────────────────────
const schema = z.object({
  currentPassword: z.string().min(1, 'Current password is required'),
  newPassword: z
    .string()
    .min(8, 'Password must be at least 8 characters')
    .regex(/[A-Z]/, 'Must contain an uppercase letter')
    .regex(/[0-9]/, 'Must contain a number')
    .regex(/[^a-zA-Z0-9]/, 'Must contain a special character'),
  confirmPassword: z.string().min(1, 'Please confirm your new password'),
}).refine(
  (data) => data.newPassword === data.confirmPassword,
  { message: 'Passwords do not match', path: ['confirmPassword'] }
).refine(
  (data) => data.currentPassword !== data.newPassword,
  { message: 'New password must be different from current password', path: ['newPassword'] }
);

type FormData = z.infer<typeof schema>;

// ── Component ────────────────────────────────────────────────────────────────
const ChangePasswordPage: React.FC = () => {
  const navigate = useNavigate();
  const { logout } = useAuthStore();
  const queryClient = useQueryClient();

  // Show/hide password toggles
  const [showCurrent, setShowCurrent] = React.useState(false);
  const [showNew,     setShowNew]     = React.useState(false);
  const [showConfirm, setShowConfirm] = React.useState(false);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormData>({ resolver: zodResolver(schema) });

  const mutation = useMutation({
    mutationFn: (data: FormData) =>
      axiosClient.post('/auth/change-password', data),
    onSuccess: () => {
      toast.success('Password changed! Please log in again.');
      reset();
      // Logout — refresh tokens were invalidated on backend 
      setTimeout(() => {
        logout();
        queryClient.clear();
        navigate('/login');
      }, 2000);
    },
    onError: (error: any) => {
      const message =
        error.response?.data?.message ||
        error.response?.data?.errors?.[0] ||
        'Failed to change password. Please try again.';
      toast.error(message);
    },
  });

  function PasswordField({
    label,
    fieldName,
    show,
    onToggle,
    placeholder,
    error,
  }: {
    label: string;
    fieldName: keyof FormData;
    show: boolean;
    onToggle: () => void;
    placeholder: string;
    error?: string;
  }) {
    return (
      <div>
        <label className="block text-xs font-medium text-gray-700 mb-1">
          {label} <span className="text-red-500">*</span>
        </label>
        <div className="relative">
          <input
            {...register(fieldName)}
            type={show ? 'text' : 'password'}
            placeholder={placeholder}
            className="w-full px-4 py-2.5 pr-10 text-sm border border-gray-200
                       rounded-xl focus:outline-none focus:ring-2 focus:ring-indigo-300"
          />
          <button
            type="button"
            onClick={onToggle}
            className="absolute right-3 top-1/2 -translate-y-1/2
                       text-gray-400 hover:text-gray-600"
          >
            {show ? <EyeOff size={15} /> : <Eye size={15} />}
          </button>
        </div>
        {error && <p className="text-xs text-red-500 mt-1">{error}</p>}
      </div>
    );
  }

  return (
    <Layout>
      <div className="max-w-md mx-auto">

        {/* Header */}
        <div className="mb-6">
          <div className="flex items-center gap-3 mb-2">
            <div className="h-10 w-10 rounded-xl bg-indigo-100
                            flex items-center justify-center">
              <Lock size={18} className="text-indigo-600" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-gray-900">Change Password</h1>
              <p className="text-xs text-gray-400">
                You will be logged out after changing your password
              </p>
            </div>
          </div>
        </div>

        {/* Form */}
        <div className="bg-white border border-gray-200 rounded-2xl p-6 shadow-sm">
          <form
            onSubmit={handleSubmit((data) => mutation.mutate(data))}
            className="space-y-4"
          >
            {/* Current Password */}
            <PasswordField
              label="Current Password"
              fieldName="currentPassword"
              show={showCurrent}
              onToggle={() => setShowCurrent(!showCurrent)}
              placeholder="Enter your current password"
              error={errors.currentPassword?.message}
            />

            {/* Divider */}
            <div className="border-t border-gray-100 pt-2" />

            {/* New Password */}
            <PasswordField
              label="New Password"
              fieldName="newPassword"
              show={showNew}
              onToggle={() => setShowNew(!showNew)}
              placeholder="Min 8 chars, uppercase, number, special"
              error={errors.newPassword?.message}
            />

            {/* Confirm Password */}
            <PasswordField
              label="Confirm New Password"
              fieldName="confirmPassword"
              show={showConfirm}
              onToggle={() => setShowConfirm(!showConfirm)}
              placeholder="Re-enter new password"
              error={errors.confirmPassword?.message}
            />

            {/* Password requirements hint */}
            <div className="bg-gray-50 rounded-xl p-3">
              <p className="text-xs font-medium text-gray-500 mb-1">
                Password must contain:
              </p>
              <div className="grid grid-cols-2 gap-1 text-xs text-gray-400">
                <span>✓ At least 8 characters</span>
                <span>✓ One uppercase letter</span>
                <span>✓ One number</span>
                <span>✓ One special character</span>
              </div>
            </div>

            {/* Buttons */}
            <div className="flex items-center gap-3 pt-2">
              <button
                type="button"
                onClick={() => navigate(-1)}
                className="flex-1 py-2.5 border border-gray-200 text-gray-600
                           text-sm font-medium rounded-xl hover:bg-gray-50
                           transition-colors"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={mutation.isPending}
                className="flex-1 py-2.5 bg-indigo-600 text-white text-sm
                           font-medium rounded-xl hover:bg-indigo-700
                           transition-colors disabled:opacity-50
                           disabled:cursor-not-allowed"
              >
                {mutation.isPending ? 'Changing...' : 'Change Password'}
              </button>
            </div>
          </form>
        </div>

      </div>
    </Layout>
  );
};

export default ChangePasswordPage;