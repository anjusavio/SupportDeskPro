/**
 * RegisterPage — creates new customer account.
 * 
 * CONCEPTS:
 * 
 * 1. Controlled vs Uncontrolled Components
 *    Controlled   → React controls input value via state
 *    Uncontrolled → DOM controls input value (useRef)
 *    React Hook Form uses uncontrolled inputs internally
 *    for better performance — no re-render on every keystroke
 * 
 * 2. Zod Schema Validation
 *    .min(8) → minimum 8 characters
 *    .regex() → must match pattern -email
 *    .refine() → custom validation (password match)
 *    All errors shown automatically on submit
 * 
 * 3. async/await vs .then()
 *    Both handle Promises — async/await is cleaner
 *    try/catch handles errors like C# try/catch
 * 
 * 4. useNavigate — programmatic redirect after register.
 */
import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { registerApi } from '../../api/authApi';

/**
 * Zod validation schema for registration form.
 * 
 * CONCEPTS: .refine() adds custom cross-field validation.
 * Used here to check password === confirmPassword.
 * Cannot do this with simple field rules alone.
 */
const registerSchema = z
  .object({
    firstName: z
      .string()
      .min(1, 'First name is required')
      .max(50, 'First name too long'),

    lastName: z
      .string()
      .min(1, 'Last name is required')
      .max(50, 'Last name too long'),

    email: z
      .string()
      .min(1, 'Email is required')
      .email('Please enter a valid email'),

    password: z
      .string()
      .min(8, 'Password must be at least 8 characters')
      .regex(/[A-Z]/, 'Password must contain at least one uppercase letter')
      .regex(/[0-9]/, 'Password must contain at least one number')
      .regex(
        /[^a-zA-Z0-9]/,
        'Password must contain at least one special character'
      ),

    confirmPassword: z.string().min(1, 'Please confirm your password'),

    tenantSlug: z
      .string()
      .min(1, 'Company slug is required')
      .max(50, 'Company slug too long')
      .regex(
        /^[a-z0-9-]+$/,
        'Slug can only contain lowercase letters, numbers and hyphens'
      ),
  })
  /**
   * .refine() — cross-field validation
   * Checks if password matches confirmPassword
   * path: ['confirmPassword'] → shows error on confirmPassword field
   */
  .refine((data) => data.password === data.confirmPassword, {
    message: 'Passwords do not match',
    path: ['confirmPassword'],
  });

// TypeScript type inferred from Zod schema automatically
type RegisterFormData = z.infer<typeof registerSchema>;

//React.FunctionComponent without props - it MUST return JSX
const RegisterPage: React.FC = () => {
  const navigate = useNavigate();
  const [isLoading, setIsLoading] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
  });

  /**
   * onSubmit — called only after all Zod validations pass.
   * Calls register API → redirects to login on success.
   */
  const onSubmit = async (data: RegisterFormData) => {
    setIsLoading(true);
    try {
      const response = await registerApi({
        firstName: data.firstName,
        lastName: data.lastName,
        email: data.email,
        password: data.password,
        confirmPassword: data.confirmPassword,
        tenantSlug: data.tenantSlug,
      });


      if (response.success) {
        toast.success('Account created! Please sign in.');
        navigate('/login');
      }
    } catch (error: any) {
      /**
       * Handle RFC 7807 Problem Details error from backend.
       * error.response.data contains the Problem Details object.
       * detail field has the human-readable error message.
       */
      const message =
        error.response?.data?.detail ||
        error.response?.data?.message ||
        'Registration failed. Please try again.';
      toast.error(message);
    } finally {
      // Always runs — clears loading state whether success or error
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center py-12">
      <div className="bg-white p-8 rounded-xl shadow-md w-full max-w-lg">

        {/* Header */}
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-gray-900">
            Create Account
          </h1>
          <p className="text-gray-500 mt-2">
            Register as a customer to submit support tickets
          </p>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">

          {/* First Name + Last Name — side by side */}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                First Name
              </label>
              <input
                {...register('firstName')}
                type="text"
                placeholder="John"
                className="w-full px-4 py-2 border border-gray-300 rounded-lg
                           focus:outline-none focus:ring-2 focus:ring-blue-500
                           focus:border-transparent"
              />
              {errors.firstName && (
                <p className="text-red-500 text-sm mt-1">
                  {errors.firstName.message}
                </p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Last Name
              </label>
              <input
                {...register('lastName')}
                type="text"
                placeholder="Doe"
                className="w-full px-4 py-2 border border-gray-300 rounded-lg
                           focus:outline-none focus:ring-2 focus:ring-blue-500
                           focus:border-transparent"
              />
              {errors.lastName && (
                <p className="text-red-500 text-sm mt-1">
                  {errors.lastName.message}
                </p>
              )}
            </div>
          </div>

          {/* Email */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Email Address
            </label>
            <input
              {...register('email')}
              type="email"
              placeholder="you@company.com"
              className="w-full px-4 py-2 border border-gray-300 rounded-lg
                         focus:outline-none focus:ring-2 focus:ring-blue-500
                         focus:border-transparent"
            />
            {errors.email && (
              <p className="text-red-500 text-sm mt-1">
                {errors.email.message}
              </p>
            )}
          </div>

          {/* Company Slug */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Company Slug
            </label>
            <input
              {...register('tenantSlug')}
              type="text"
              placeholder="acme-corp"
              className="w-full px-4 py-2 border border-gray-300 rounded-lg
                         focus:outline-none focus:ring-2 focus:ring-blue-500
                         focus:border-transparent"
            />
            {/* Helper text — explains what slug is */}
            <p className="text-gray-400 text-xs mt-1">
              Your company identifier — lowercase, numbers and hyphens only
            </p>
            {errors.tenantSlug && (
              <p className="text-red-500 text-sm mt-1">
                {errors.tenantSlug.message}
              </p>
            )}
          </div>

          {/* Password */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Password
            </label>
            <input
              {...register('password')}
              type="password"
              placeholder="••••••••"
              className="w-full px-4 py-2 border border-gray-300 rounded-lg
                         focus:outline-none focus:ring-2 focus:ring-blue-500
                         focus:border-transparent"
            />
            {errors.password && (
              <p className="text-red-500 text-sm mt-1">
                {errors.password.message}
              </p>
            )}
          </div>

          {/* Confirm Password */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Confirm Password
            </label>
            <input
              {...register('confirmPassword')}
              type="password"
              placeholder="••••••••"
              className="w-full px-4 py-2 border border-gray-300 rounded-lg
                         focus:outline-none focus:ring-2 focus:ring-blue-500
                         focus:border-transparent"
            />
            {errors.confirmPassword && (
              <p className="text-red-500 text-sm mt-1">
                {errors.confirmPassword.message}
              </p>
            )}
          </div>

          {/* Submit button */}
          <button
            type="submit"
            disabled={isLoading}
            className="w-full bg-blue-600 text-white py-2 px-4 rounded-lg
                       hover:bg-blue-700 transition-colors font-medium
                       disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isLoading ? 'Creating Account...' : 'Create Account'}
          </button>
        </form>

        {/* Login link */}
        <p className="text-center text-gray-500 text-sm mt-6">
          Already have an account?{' '}
          <a href="/login" className="text-blue-600 hover:underline">
            Sign in here
          </a>
        </p>
      </div>
    </div>
  );
};

export default RegisterPage;