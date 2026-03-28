/**
 * ForgotPasswordPage — customer enters email to receive reset link.
 *
 * CONCEPTS:
 *
 * 1. useMutation for POST /api/auth/forgot-password
 *    Always shows success message even if email not found.
 *    Prevents user enumeration — same UX regardless 
 *
 * 2. Show success state after submit
 *    Form replaced by success message — no redirect needed 
 */

import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { Mail, ArrowLeft, CheckCircle2 } from 'lucide-react';
import axiosClient from '../../api/axiosClient';

const schema = z.object({
  email: z.string().email('Enter a valid email address'),
});
type FormData = z.infer<typeof schema>;

const ForgotPasswordPage: React.FC = () => {
  const navigate = useNavigate();
  const [submitted, setSubmitted] = useState(false);
  const [submittedEmail, setSubmittedEmail] = useState('');

  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
  });

  const mutation = useMutation({
    mutationFn: (data: FormData) =>
      axiosClient.post('/auth/forgot-password', data),
    onSuccess: (_, variables) => {
      setSubmittedEmail(variables.email);
      setSubmitted(true); // show success state 
    },
    onError: () => toast.error('Something went wrong. Please try again.'),
  });

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-sm border border-gray-200
                      p-8 w-full max-w-md">

        {/* Back to login */}
        <button
          onClick={() => navigate('/login')}
          className="flex items-center gap-1.5 text-sm text-gray-500
                     hover:text-gray-700 mb-6 transition-colors"
        >
          <ArrowLeft size={15} /> Back to Login
        </button>

        {/* Success state */}
        {submitted ? (
          <div className="text-center">
            <div className="flex justify-center mb-4">
              <CheckCircle2 size={48} className="text-green-500" />
            </div>
            <h1 className="text-xl font-bold text-gray-900 mb-2">
              Check your email
            </h1>
            <p className="text-sm text-gray-500 mb-1">
              We sent a password reset link to:
            </p>
            <p className="text-sm font-semibold text-indigo-600 mb-6">
              {submittedEmail}
            </p>
            <p className="text-xs text-gray-400 mb-6">
              The link expires in 1 hour. Check your spam folder if you
              don't see it.
            </p>
            <button
              onClick={() => navigate('/login')}
              className="w-full py-2.5 bg-indigo-600 text-white text-sm
                         font-medium rounded-xl hover:bg-indigo-700 transition-colors"
            >
              Back to Login
            </button>
          </div>
        ) : (
          // Form state
          <>
            <div className="mb-6">
              <div className="flex justify-center mb-4">
                <div className="h-12 w-12 rounded-xl bg-indigo-100
                                flex items-center justify-center">
                  <Mail size={22} className="text-indigo-600" />
                </div>
              </div>
              <h1 className="text-xl font-bold text-gray-900 text-center mb-1">
                Forgot Password?
              </h1>
              <p className="text-sm text-gray-500 text-center">
                Enter your email and we'll send you a reset link.
              </p>
            </div>

            <form
              onSubmit={handleSubmit((data) => mutation.mutate(data))}
              className="space-y-4"
            >
              <div>
                <label className="block text-xs font-medium text-gray-700 mb-1">
                  Email Address
                </label>
                <input
                  {...register('email')}
                  type="email"
                  placeholder="your@email.com"
                  className="w-full px-4 py-2.5 text-sm border border-gray-200
                             rounded-xl focus:outline-none focus:ring-2
                             focus:ring-indigo-300"
                />
                {errors.email && (
                  <p className="text-xs text-red-500 mt-1">
                    {errors.email.message}
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
                {mutation.isPending ? 'Sending...' : 'Send Reset Link'}
              </button>
            </form>
          </>
        )}
      </div>
    </div>
  );
};

export default ForgotPasswordPage;