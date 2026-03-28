/**
 * ResetPasswordPage — user sets new password using token from email.
 *
 * CONCEPTS:
 *
 * 1. useSearchParams reads ?token=xxx from URL
 *    User clicks reset link → lands here with token 
 *
 * 2. Zod refine — cross-field validation
 *    NewPassword must match ConfirmPassword 
 *
 * 3. On success → redirect to login 
 */

import React from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { Lock, CheckCircle2 } from 'lucide-react';
import axiosClient from '../../api/axiosClient';

const schema = z.object({
  newPassword: z
    .string()
    .min(8, 'Password must be at least 8 characters')
    .regex(/[A-Z]/, 'Must contain an uppercase letter')
    .regex(/[0-9]/, 'Must contain a number')
    .regex(/[^a-zA-Z0-9]/, 'Must contain a special character'),
  confirmPassword: z.string().min(1, 'Please confirm your password'),
}).refine(
  (data) => data.newPassword === data.confirmPassword,
  { message: 'Passwords do not match', path: ['confirmPassword'] }
);
type FormData = z.infer<typeof schema>;

const ResetPasswordPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get('token');

  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
  });

  const mutation = useMutation({
    mutationFn: (data: FormData) =>
      axiosClient.post('/auth/reset-password', {
         token: token,
        newPassword:     data.newPassword,
        confirmPassword: data.confirmPassword,
      }),
    onSuccess: () => {
      toast.success('Password reset successfully!');
      setTimeout(() => navigate('/login'), 2000);
    },
    onError: (error: any) => {
      const message =
        error.response?.data?.message ||
        'Reset failed. The link may have expired.';
      toast.error(message);
    },
  });

  // No token in URL
  if (!token) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center
                      justify-center p-4">
        <div className="bg-white rounded-2xl shadow-sm border border-gray-200
                        p-8 w-full max-w-md text-center">
          <p className="text-gray-500 mb-4">Invalid reset link.</p>
          <button
            onClick={() => navigate('/forgot-password')}
            className="text-sm text-indigo-600 hover:underline"
          >
            Request a new reset link
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 flex items-center
                    justify-center p-4">
      <div className="bg-white rounded-2xl shadow-sm border border-gray-200
                      p-8 w-full max-w-md">

        {/* Success state */}
        {mutation.isSuccess ? (
          <div className="text-center">
            <div className="flex justify-center mb-4">
              <CheckCircle2 size={48} className="text-green-500" />
            </div>
            <h1 className="text-xl font-bold text-gray-900 mb-2">
              Password Reset!
            </h1>
            <p className="text-sm text-gray-500 mb-6">
              Your password has been reset. Redirecting to login...
            </p>
            <button
              onClick={() => navigate('/login')}
              className="w-full py-2.5 bg-indigo-600 text-white text-sm
                         font-medium rounded-xl hover:bg-indigo-700
                         transition-colors"
            >
              Go to Login
            </button>
          </div>
        ) : (
          // Form state
          <>
            <div className="mb-6">
              <div className="flex justify-center mb-4">
                <div className="h-12 w-12 rounded-xl bg-indigo-100
                                flex items-center justify-center">
                  <Lock size={22} className="text-indigo-600" />
                </div>
              </div>
              <h1 className="text-xl font-bold text-gray-900 text-center mb-1">
                Reset Password
              </h1>
              <p className="text-sm text-gray-500 text-center">
                Enter your new password below.
              </p>
            </div>

            <form
              onSubmit={handleSubmit((data) => mutation.mutate(data))}
              className="space-y-4"
            >
              {/* New Password */}
              <div>
                <label className="block text-xs font-medium text-gray-700 mb-1">
                  New Password
                </label>
                <input
                  {...register('newPassword')}
                  type="password"
                  placeholder="Min 8 chars, uppercase, number, special"
                  className="w-full px-4 py-2.5 text-sm border border-gray-200
                             rounded-xl focus:outline-none focus:ring-2
                             focus:ring-indigo-300"
                />
                {errors.newPassword && (
                  <p className="text-xs text-red-500 mt-1">
                    {errors.newPassword.message}
                  </p>
                )}
              </div>

              {/* Confirm Password */}
              <div>
                <label className="block text-xs font-medium text-gray-700 mb-1">
                  Confirm Password
                </label>
                <input
                  {...register('confirmPassword')}
                  type="password"
                  placeholder="Re-enter new password"
                  className="w-full px-4 py-2.5 text-sm border border-gray-200
                             rounded-xl focus:outline-none focus:ring-2
                             focus:ring-indigo-300"
                />
                {errors.confirmPassword && (
                  <p className="text-xs text-red-500 mt-1">
                    {errors.confirmPassword.message}
                  </p>
                )}
              </div>

              <button
                type="submit"
                disabled={mutation.isPending}
                className="w-full py-2.5 bg-indigo-600 text-white text-sm
                           font-medium rounded-xl hover:bg-indigo-700
                           transition-colors disabled:opacity-50
                           disabled:cursor-not-allowed"
              >
                {mutation.isPending ? 'Resetting...' : 'Reset Password'}
              </button>
            </form>
          </>
        )}
      </div>
    </div>
  );
};

export default ResetPasswordPage;