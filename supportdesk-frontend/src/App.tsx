/**
 * App.tsx — Root component with route configuration.
 * 
 * CONCEPTS:
 * 
 * 1. React Router — client-side navigation.
 *    No full page reload — just component swap 
 * 
 * 2. Protected Routes — checks if user is authenticated.
 *    Redirects to login if not logged in.
 *    Redirects based on role (Admin/Agent/Customer).
 * 
 * 3. Lazy Loading — loads page components only when needed.
 *    Reduces initial bundle size → faster first load 
 * 
 * 4.QueryClientProvider — wraps entire app so any component
 *    Reduces initial bundle size → faster first load
 *    can use React Query hooks for API data fetching.
 */
import React from 'react';
import {
  BrowserRouter,
  Routes,
  Route,
  Navigate,
} from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'react-hot-toast';
import useAuthStore from './store/authStore';
import TicketDetailPage from './pages/customer/TicketDetailPage';

// Pages
import LoginPage from './pages/auth/LoginPage';
import RegisterPage from './pages/auth/RegisterPage';

// Admin pages
import AdminDashboardPage from './pages/admin/DashboardPage';
import TicketsPage from './pages/admin/TicketsPage';

// Customer pages
import MyTicketsPage from './pages/customer/MyTicketsPage';
import CreateTicketPage from './pages/customer/CreateTicketPage';

/**
 * QueryClient — React Query configuration.
 * staleTime: how long cached data is considered fresh (5 mins)
 * retry: how many times to retry failed requests
 */
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // 5 minutes
      retry: 1,
    },
  },
});

/**
 * ProtectedRoute — wraps routes that require authentication.
 * * 
 * CONCEPT: Higher Order Component (HOC) pattern.
 * Wraps any component and adds authentication check.
 * Reusable — used for every protected page 
 *
 * Flow:
 * Not logged in → redirect to /login
 * Wrong role    → redirect to their home page
 * Correct role  → render the page 
 */
const ProtectedRoute = ({
  children,
  allowedRoles,
}: {
  children: React.ReactNode;
  allowedRoles?: string[];
}) => {
  const { isAuthenticated, user } = useAuthStore();

  // Not logged in → redirect to login
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  // Wrong role → redirect to their home
  if (allowedRoles && user && !allowedRoles.includes(user.role)) {
    if (user.role === 'Customer') return <Navigate to="/my-tickets" replace />;
    if (user.role === 'Agent') return <Navigate to ="/tickets" replace />;
    if (user.role === 'Admin') return <Navigate to="/dashboard" replace />;
    return <Navigate to="/dashboard" replace />;
  }

  return <>{children}</>;
};

function App() {
  return (
    /**
     * QueryClientProvider — makes React Query available to all components.
     * BrowserRouter — enables client-side routing.
     * Toaster — shows toast notifications anywhere in app.
     */
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          {/* Public routes — no auth required */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />

          {/* Admin routes */}
          <Route path="/dashboard" element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <AdminDashboardPage />
            </ProtectedRoute>
          } />

          <Route path="/tickets" element={
            <ProtectedRoute allowedRoles={['Admin', 'Agent']}>
              <TicketsPage />
            </ProtectedRoute>
          } />

          {/* Customer routes */}
          <Route path="/my-tickets" element={
            <ProtectedRoute allowedRoles={['Customer']}>
              <MyTicketsPage />
            </ProtectedRoute>
          } />

          <Route path="/create-ticket" element={
            <ProtectedRoute allowedRoles={['Customer']}>
              <CreateTicketPage />
            </ProtectedRoute>
          } />

          {/* Customer ticket detail */}
            <Route path="/my-tickets/:id" element={
              <ProtectedRoute allowedRoles={['Customer']}>
                <TicketDetailPage />   {/* ← we will build this next */}
              </ProtectedRoute>
            } />

          {/* Default redirect */}
          <Route path="/" element={<Navigate to="/login" replace />} />
          <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
      </BrowserRouter>

      {/* Toast notifications — shows success/error messages */}
      <Toaster position="top-right" />
    </QueryClientProvider>
  );
}

export default App;