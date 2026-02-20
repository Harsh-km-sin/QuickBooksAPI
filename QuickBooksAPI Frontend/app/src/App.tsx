import { Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from '@/context/AuthContext';
import { ThemeProvider } from '@/components/theme-provider';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { MainLayout } from '@/components/MainLayout';
import { Toaster } from '@/components/ui/sonner';
import {
  Login,
  Register,
  Dashboard,
  ConnectedCompanies,
  Customers,
  ProductsPage,
  Vendors,
  Bills,
  Invoices,
  ChartOfAccounts,
  JournalEntries,
} from '@/pages';

function renderProtectedPage(page: React.ReactNode) {
  return (
    <ThemeProvider defaultTheme="system" storageKey="qb-connect-theme">
      <AuthProvider>
        <ProtectedRoute>
          <MainLayout>{page}</MainLayout>
        </ProtectedRoute>
        <Toaster position="top-right" richColors />
      </AuthProvider>
    </ThemeProvider>
  );
}

function App() {
  return (
    <Routes>
      <Route
        path="/login"
        element={
          <ThemeProvider defaultTheme="system" storageKey="qb-connect-theme">
            <AuthProvider>
              <Login />
              <Toaster position="top-right" richColors />
            </AuthProvider>
          </ThemeProvider>
        }
      />
      <Route
        path="/register"
        element={
          <ThemeProvider defaultTheme="system" storageKey="qb-connect-theme">
            <AuthProvider>
              <Register />
              <Toaster position="top-right" richColors />
            </AuthProvider>
          </ThemeProvider>
        }
      />
      <Route path="/" element={renderProtectedPage(<ConnectedCompanies />)} />
      <Route path="/dashboard" element={renderProtectedPage(<Dashboard />)} />
      <Route path="/connected-companies" element={<Navigate to="/" replace />} />
      <Route path="/customers" element={renderProtectedPage(<Customers />)} />
      <Route path="/products" element={renderProtectedPage(<ProductsPage />)} />
      <Route path="/vendors" element={renderProtectedPage(<Vendors />)} />
      <Route path="/bills" element={renderProtectedPage(<Bills />)} />
      <Route path="/invoices" element={renderProtectedPage(<Invoices />)} />
      <Route path="/chart-of-accounts" element={renderProtectedPage(<ChartOfAccounts />)} />
      <Route path="/journal-entries" element={renderProtectedPage(<JournalEntries />)} />
      <Route path="*" element={<Navigate to="/login" replace />} />
    </Routes>
  );
}

export default App;
