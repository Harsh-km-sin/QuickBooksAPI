import { useState, useEffect, useCallback } from 'react';
import { customerApi, productApi, vendorApi, billApi, invoiceApi } from '@/api/client';
import type { DashboardStats } from '@/types';

interface UseDashboardStatsReturn {
  stats: DashboardStats | null;
  isLoading: boolean;
  error: string | null;
  refetch: () => Promise<void>;
}

export function useDashboardStats(): UseDashboardStatsReturn {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchStats = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);

      // Fetch all data in parallel
      const [customersRes, productsRes, vendorsRes, billsRes, invoicesRes] = await Promise.all([
        customerApi.list(),
        productApi.list(),
        vendorApi.list(),
        billApi.list(),
        invoiceApi.list(),
      ]);

      // Calculate stats (use totalCount from paged response for accurate counts)
      const customers = customersRes.success ? customersRes.data?.items ?? [] : [];
      const products = productsRes.success ? productsRes.data?.items ?? [] : [];
      const vendors = vendorsRes.success ? vendorsRes.data?.items ?? [] : [];
      const bills = billsRes.success ? billsRes.data?.items ?? [] : [];
      const invoices = invoicesRes.success ? invoicesRes.data?.items ?? [] : [];
      const customersCount = customersRes.success && customersRes.data ? customersRes.data.totalCount : customers.length;
      const productsCount = productsRes.success && productsRes.data ? productsRes.data.totalCount : products.length;
      const vendorsCount = vendorsRes.success && vendorsRes.data ? vendorsRes.data.totalCount : vendors.length;
      const billsCount = billsRes.success && billsRes.data ? billsRes.data.totalCount : bills.length;
      const invoicesCount = invoicesRes.success && invoicesRes.data ? invoicesRes.data.totalCount : invoices.length;

      const totalInvoiceAmount = invoices.reduce((sum, inv) => sum + (inv.totalAmt || 0), 0);
      const totalBillAmount = bills.reduce((sum, bill) => sum + (bill.totalAmt || 0), 0);
      const outstandingInvoiceBalance = invoices.reduce((sum, inv) => sum + (inv.balance || 0), 0);
      const outstandingBillBalance = bills.reduce((sum, bill) => sum + (bill.balance || 0), 0);

      setStats({
        customersCount,
        productsCount,
        vendorsCount,
        billsCount,
        invoicesCount,
        totalInvoiceAmount,
        totalBillAmount,
        outstandingInvoiceBalance,
        outstandingBillBalance,
      });
    } catch (err) {
      const message = err instanceof Error ? err.message : 'An unexpected error occurred';
      setError(message);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchStats();
  }, [fetchStats]);

  return {
    stats,
    isLoading,
    error,
    refetch: fetchStats,
  };
}
