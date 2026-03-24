/**
 * LoginPage — authenticates user and redirects based on role.
 * 
 * CONCEPTS:
 * 
 * 1. React Hook Form — manages form state, validation and submission.
 *    Without it: manual useState for every field, manual validation.
 *    With it: one line per field, built-in validation, error messages automatically handled.
 * 
 * 2. Zod Schema — defines validation rules as TypeScript types.
 *    z.string().email() → must be valid email
 *    z.string().min(6)  → minimum 6 characters
 * 
 * 3. useState — local component state for loading/error.
 *    Re-renders component when state changes.
 * 
 * 4. useNavigate — programmatic navigation after login.
 * 
 * 5. Role-based redirect — Admin→/dashboard, Customer→/my-tickets
 */
import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { loginApi } from '../../api/authApi';
import useAuthStore from '../../store/authStore';

/**
 * Zod validation schema for login form.
 * zodResolver connects this schema to React Hook Form.
 * Errors automatically shown on submit if rules not met.
 */
const loginSchema = z.object({
  email: z
    .string()
    .min(1, 'Email is required')
    .email('Please enter a valid email'),
  password: z
    .string()
    .min(1, 'Password is required'),
});

// TypeScript type inferred from Zod schema
type LoginFormData = z.infer<typeof loginSchema>;

const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const { setAuth } = useAuthStore();
  const [isLoading, setIsLoading] = useState(false);

  /**
   * useForm — initializes form with Zod validation.
   * register  → connects input field to form
   * handleSubmit → validates then calls onSubmit
   * formState.errors → validation error messages
   */
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  });

  /**
   * onSubmit — called after validation passes.
   * Calls login API → saves token → redirects by role.
   */
  const onSubmit = async (data: LoginFormData) => {
    setIsLoading(true);
    try {
      const response = await loginApi(data);

      if (response.success && response.data) {
        // Save token and user to Zustand store + localStorage
        setAuth(response.data.user, response.data.accessToken);

        toast.success(`Welcome back, ${response.data.user.firstName}!`);

        // Redirect based on role
        const role = response.data.user.role;
        if (role === 'Admin') navigate('/dashboard');
        else if (role === 'Agent') navigate('/agent-dashboard');
        else navigate('/my-tickets');
      }
    } catch (error: any) {
      // Show error from API response
      const message = error.response?.data?.detail
        || 'Invalid email or password';
      toast.error(message);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center">
      <div className="bg-white p-8 rounded-xl shadow-md w-full max-w-md">

        {/* Header */}
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-gray-900">
            SupportDesk Pro
          </h1>
          <p className="text-gray-500 mt-2">
            Sign in to your account
          </p>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">

          {/* Email field */}
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
            {/* Show validation error */}
            {errors.email && (
              <p className="text-red-500 text-sm mt-1">
                {errors.email.message}
              </p>
            )}
          </div>

          {/* Password field */}
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

          {/* Submit button */}
          <button
            type="submit"
            disabled={isLoading}
            className="w-full bg-blue-600 text-white py-2 px-4 rounded-lg
                       hover:bg-blue-700 transition-colors font-medium
                       disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isLoading ? 'Signing in...' : 'Sign In'}
          </button>
        </form>

        {/* Register link */}
        <p className="text-center text-gray-500 text-sm mt-6">
          Don't have an account?{' '}
          <a href="/register" className="text-blue-600 hover:underline">
            Register here
          </a>
        </p>
      </div>
    </div>
  );
};

export default LoginPage;