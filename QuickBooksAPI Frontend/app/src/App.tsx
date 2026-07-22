import { Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, Login, Register } from '@/features/auth';
import { ThemeProvider } from '@/components/theme-provider';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { MainLayout } from '@/components/MainLayout';
import { Toaster } from '@/components/ui/sonner';
import {
  Dashboard,
  ConnectedCompanies,
  Customers,
  ProductsPage,
  Vendors,
  Bills,
  Invoices,
  ChartOfAccounts,
  JournalEntries,
  Settings,
  UserManagement,
  MasterData,
  ProfitAndLoss,
  BalanceSheet,
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
      <Route path="/" element={renderProtectedPage(<Dashboard />)} />
      <Route path="/dashboard" element={<Navigate to="/" replace />} />
      <Route path="/connected-companies" element={<Navigate to="/settings/connected-companies" replace />} />
      {/* Top-level transactional pages */}
      <Route path="/invoices" element={renderProtectedPage(<Invoices />)} />
      <Route path="/bills" element={renderProtectedPage(<Bills />)} />
      <Route path="/journal-entries" element={renderProtectedPage(<JournalEntries />)} />
      <Route path="/reports/profit-and-loss" element={renderProtectedPage(<ProfitAndLoss />)} />
      <Route path="/reports/balance-sheet" element={renderProtectedPage(<BalanceSheet />)} />
      {/* Settings hub */}
      <Route path="/settings" element={renderProtectedPage(<Settings />)} />
      <Route path="/settings/user-management" element={renderProtectedPage(<UserManagement />)} />
      <Route path="/settings/master-data" element={renderProtectedPage(<MasterData />)} />
      <Route path="/settings/master-data/customers" element={renderProtectedPage(<Customers />)} />
      <Route path="/settings/master-data/vendors" element={renderProtectedPage(<Vendors />)} />
      <Route path="/settings/master-data/products" element={renderProtectedPage(<ProductsPage />)} />
      <Route path="/settings/master-data/chart-of-accounts" element={renderProtectedPage(<ChartOfAccounts />)} />
      <Route path="/settings/connected-companies" element={renderProtectedPage(<ConnectedCompanies />)} />
      {/* Bills/Invoices/Journal Entries moved out of Master Data to top-level nav */}
      <Route path="/settings/master-data/bills" element={<Navigate to="/bills" replace />} />
      <Route path="/settings/master-data/invoices" element={<Navigate to="/invoices" replace />} />
      <Route path="/settings/master-data/journal-entries" element={<Navigate to="/journal-entries" replace />} />
      {/* Old entity routes redirect to master data */}
      <Route path="/customers" element={<Navigate to="/settings/master-data" replace />} />
      <Route path="/vendors" element={<Navigate to="/settings/master-data" replace />} />
      <Route path="/products" element={<Navigate to="/settings/master-data" replace />} />
      <Route path="/chart-of-accounts" element={<Navigate to="/settings/master-data" replace />} />
      <Route path="*" element={<Navigate to="/login" replace />} />
    </Routes>
  );
}

export default App;
