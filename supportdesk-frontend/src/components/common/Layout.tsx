/**
 * Layout — wrapper component that adds Navbar to every protected page.
 *
 * CONCEPT: Layout Pattern / Composition Pattern
 * Instead of adding Navbar in every page component separately,
 * wrap pages in Layout component — Navbar added automatically.
 *
 * children prop → whatever is between <Layout> tags is rendered here.
 * Same concept as C# template method pattern 
 *
 * Usage:
 * <Layout>
 *   <DashboardPage />   ← rendered as children
 * </Layout>
 */
import React from 'react';
import Navbar from './Navbar';

interface LayoutProps {
  children: React.ReactNode;
}

const Layout: React.FC<LayoutProps> = ({ children }) => {
  return (
    <div className="min-h-screen bg-gray-50">
      {/* Navbar shown at top of every page */}
      <Navbar />

      {/* Page content rendered below Navbar */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {children}
      </main>
    </div>
  );
};

export default Layout;
