/**
 * Navbar — top navigation bar shown on all protected pages.
 *
 * CONCEPTS:
 *
 * 1. useQuery (React Query)
 *    Fetches and CACHES data automatically.
 *    refetchInterval → polls API every 30 seconds for new notifications.
 *    No need to manually manage loading/error state
 *
 * 2. Conditional Rendering
 *    {user.role === 'Admin' && <Link>Dashboard</Link>}
 *    Shows menu items based on role — same as C# if/else
 *    but inline in JSX
 *
 * 3. useState for dropdown
 *    Local state — only Navbar needs to know if dropdown is open.
 *    No need for global state (Zustand) for this
 *
 * 4. useNavigate — programmatic redirect on logout.
 *
 * 5. Props — data passed from parent to child component.
 *    Like C# method parameters 
 */
import React, { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  Bell,
  LogOut,
  Ticket,
  LayoutDashboard,
  Users,
  ChevronDown,
  Menu,
  X,
} from 'lucide-react';
import useAuthStore from '../../store/authStore';
import { getUnreadCountApi } from '../../api/notificationApi';

const Navbar: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout } = useAuthStore();

  // Local state for mobile menu and user dropdown
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [isUserDropdownOpen, setIsUserDropdownOpen] = useState(false);

  /**
   * useQuery — fetches unread notification count.
   *
   * CONCEPT: React Query useQuery
   * queryKey  → unique identifier for this query cache
   * queryFn   → function that fetches data
   * refetchInterval → auto-refetch every 30 seconds
   * enabled   → only fetch if user is logged in
   *
   * Replaces: useState + useEffect + fetch + loading/error handling
   * React Query handles all of that automatically
   */
  const { data: notificationData } = useQuery({
    queryKey: ['unreadCount'],
    queryFn: getUnreadCountApi,
    refetchInterval: 30000, // poll every 30 seconds
    enabled: !!user,        // only run if user exists
  });

  const unreadCount = notificationData?.data?.count ?? 0;

  /**
   * logout — clears Zustand store and redirects to login.
   * Zustand logout() clears localStorage token automatically.
   */
  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  /**
   * isActivePage — checks if current route matches link.
   * Used to highlight active nav item.
   */
  const isActivePage = (path: string) => location.pathname === path;

  /**
   * navLinkClass — returns Tailwind classes for nav links.
   * Active page gets blue highlight, others get gray hover.
   */
  const navLinkClass = (path: string) =>
    `flex items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium
     transition-colors ${
       isActivePage(path)
         ? 'bg-blue-100 text-blue-700'
         : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900'
     }`;

  return (
    <nav className="bg-white border-b border-gray-200 sticky top-0 z-50">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">

          {/* LEFT — Logo */}
          <div className="flex items-center gap-8">
            <Link to="/" className="flex items-center gap-2">
              <Ticket className="text-blue-600" size={24} />
              <span className="font-bold text-gray-900 text-lg">
                SupportDesk Pro
              </span>
            </Link>

            {/* Desktop Navigation Links */}
            <div className="hidden md:flex items-center gap-1">

              {/**
               * Role-based navigation — only show relevant links.
               * Concept: Conditional rendering with && operator.
               * condition && <Component/> → renders only if condition true
               */}

              {/* Admin only */}
              {user?.role === 'Admin' && (
                <Link to="/dashboard" className={navLinkClass('/dashboard')}>
                  <LayoutDashboard size={16} />
                  Dashboard
                </Link>
              )}

              {/* Admin + Agent */}
              {(user?.role === 'Admin' || user?.role === 'Agent') && (
                <Link to="/tickets" className={navLinkClass('/tickets')}>
                  <Ticket size={16} />
                  Tickets
                </Link>
              )}

              {/* Admin only */}
              {user?.role === 'Admin' && (
                <Link to="/users" className={navLinkClass('/users')}>
                  <Users size={16} />
                  Users
                </Link>
              )}

              {/* Customer only */}
              {user?.role === 'Customer' && (
                <Link
                  to="/my-tickets"
                  className={navLinkClass('/my-tickets')}
                >
                  <Ticket size={16} />
                  My Tickets
                </Link>
              )}
            </div>
          </div>

          {/* RIGHT — Notifications + User menu */}
          <div className="flex items-center gap-3">

            {/**
             * Notification Bell with unread count badge.
             * CONCEPT: Conditional rendering with ternary operator.
             * condition ? <A/> : <B/> → renders A or B based on condition
             */}
            <button
              onClick={() => navigate('/notifications')}
              className="relative p-2 text-gray-600 hover:bg-gray-100
                         rounded-lg transition-colors"
            >
              <Bell size={20} />
              {/* Badge — only show if unread count > 0 */}
              {unreadCount > 0 && (
                <span className="absolute -top-1 -right-1 bg-red-500
                                 text-white text-xs rounded-full
                                 w-5 h-5 flex items-center justify-center
                                 font-medium">
                  {unreadCount > 99 ? '99+' : unreadCount}
                </span>
              )}
            </button>

            {/**
             * User dropdown menu.
             * CONCEPT: useState controls open/close.
             * onClick toggles boolean → re-renders component
             */}
            <div className="relative">
              <button
                onClick={() => setIsUserDropdownOpen(!isUserDropdownOpen)}
                className="flex items-center gap-2 px-3 py-2 rounded-lg
                           hover:bg-gray-100 transition-colors"
              >
                {/* Avatar circle with first letter of name */}
                <div className="w-8 h-8 rounded-full bg-blue-600
                                flex items-center justify-center
                                text-white text-sm font-medium">
                  {user?.firstName?.charAt(0).toUpperCase()}
                </div>
                <div className="hidden md:block text-left">
                  <p className="text-sm font-medium text-gray-900">
                    {user?.firstName} {user?.lastName}
                  </p>
                  <p className="text-xs text-gray-500">{user?.role}</p>
                </div>
                <ChevronDown size={16} className="text-gray-500" />
              </button>

              {/* Dropdown menu */}
              {isUserDropdownOpen && (
                <div className="absolute right-0 mt-1 w-48 bg-white
                                rounded-lg shadow-lg border border-gray-200
                                py-1 z-50">
                  {/* User info */}
                  <div className="px-4 py-2 border-b border-gray-100">
                    <p className="text-sm font-medium text-gray-900">
                      {user?.firstName} {user?.lastName}
                    </p>
                    <p className="text-xs text-gray-500">{user?.email}</p>
                  </div>

                  {/* Logout button */}
                  <button
                    onClick={handleLogout}
                    className="w-full flex items-center gap-2 px-4 py-2
                               text-sm text-red-600 hover:bg-red-50
                               transition-colors"
                  >
                    <LogOut size={16} />
                    Sign Out
                  </button>
                </div>
              )}
            </div>

            {/* Mobile menu button */}
            <button
              onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
              className="md:hidden p-2 text-gray-600 hover:bg-gray-100
                         rounded-lg"
            >
              {isMobileMenuOpen ? <X size={20} /> : <Menu size={20} />}
            </button>
          </div>
        </div>

        {/**
         * Mobile menu — shown only on small screens.
         * CONCEPT: Responsive design with Tailwind.
         * md:hidden → hidden on medium+ screens
         * hidden md:flex → shown on medium+ screens
         */}
        {isMobileMenuOpen && (
          <div className="md:hidden py-3 border-t border-gray-200
                          space-y-1">
            {user?.role === 'Admin' && (
              <Link
                to="/dashboard"
                className={navLinkClass('/dashboard')}
                onClick={() => setIsMobileMenuOpen(false)}
              >
                <LayoutDashboard size={16} />
                Dashboard
              </Link>
            )}
            {(user?.role === 'Admin' || user?.role === 'Agent') && (
              <Link
                to="/tickets"
                className={navLinkClass('/tickets')}
                onClick={() => setIsMobileMenuOpen(false)}
              >
                <Ticket size={16} />
                Tickets
              </Link>
            )}
            {user?.role === 'Admin' && (
              <Link
                to="/users"
                className={navLinkClass('/users')}
                onClick={() => setIsMobileMenuOpen(false)}
              >
                <Users size={16} />
                Users
              </Link>
            )}
            {user?.role === 'Customer' && (
              <Link
                to="/my-tickets"
                className={navLinkClass('/my-tickets')}
                onClick={() => setIsMobileMenuOpen(false)}
              >
                <Ticket size={16} />
                My Tickets
              </Link>
            )}
          </div>
        )}
      </div>
    </nav>
  );
};

export default Navbar;