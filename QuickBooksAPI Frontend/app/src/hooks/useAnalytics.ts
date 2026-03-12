import { useState, useEffect, useCallback } from 'react';
import { analyticsApi } from '@/api/client';
import type { CashRunwayResult, VendorSpend, CustomerProfitability, RevenueExpensesMonthly, Anomaly, KpiSnapshot, CloseIssue } from '@/types';

interface UseAnalyticsReturn {
  cashRunway: CashRunwayResult | null;
  vendorSpend: VendorSpend[] | null;
  customerProfitability: CustomerProfitability[] | null;
  revenueExpenses: RevenueExpensesMonthly[] | null;
  anomalies: Anomaly[] | null;
  kpis: KpiSnapshot[] | null;
  closeIssues: CloseIssue[] | null;
  isLoading: boolean;
  error: string | null;
  refetch: () => Promise<void>;
}

export function useAnalytics(): UseAnalyticsReturn {
  const [cashRunway, setCashRunway] = useState<CashRunwayResult | null>(null);
  const [vendorSpend, setVendorSpend] = useState<VendorSpend[] | null>(null);
  const [customerProfitability, setCustomerProfitability] = useState<CustomerProfitability[] | null>(null);
  const [revenueExpenses, setRevenueExpenses] = useState<RevenueExpensesMonthly[] | null>(null);
  const [anomalies, setAnomalies] = useState<Anomaly[] | null>(null);
  const [kpis, setKpis] = useState<KpiSnapshot[] | null>(null);
  const [closeIssues, setCloseIssues] = useState<CloseIssue[] | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchAnalytics = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);

      const since = new Date();
      since.setDate(since.getDate() - 30);
      const to = new Date().toISOString().slice(0, 10);
      const from = new Date();
      from.setMonth(from.getMonth() - 6);

      const [runwayRes, vendorRes, customerRes, revenueRes, anomaliesRes, kpisRes, closeIssuesRes] = await Promise.all([
        analyticsApi.getCashRunway(),
        analyticsApi.getVendorSpendTop(30, 10),
        analyticsApi.getCustomerProfitability({ top: 50 }),
        analyticsApi.getRevenueExpenses(),
        analyticsApi.getAnomalies(since.toISOString()),
        analyticsApi.getKpis({ from: from.toISOString().slice(0, 10), to, names: 'GrossMargin,RevenueGrowth,BurnMultiple' }),
        analyticsApi.getCloseIssues({ unresolvedOnly: true }),
      ]);

      if (runwayRes.success) setCashRunway(runwayRes.data);
      if (vendorRes.success) setVendorSpend(vendorRes.data);
      if (customerRes.success) setCustomerProfitability(customerRes.data);
      if (revenueRes.success) setRevenueExpenses(revenueRes.data);
      if (anomaliesRes.success) setAnomalies(anomaliesRes.data);
      if (kpisRes.success) setKpis(kpisRes.data);
      if (closeIssuesRes.success) setCloseIssues(closeIssuesRes.data);
      
    } catch (err) {
      const message = err instanceof Error ? err.message : 'An unexpected error occurred';
      setError(message);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchAnalytics();
  }, [fetchAnalytics]);

  return {
    cashRunway,
    vendorSpend,
    customerProfitability,
    revenueExpenses,
    anomalies,
    kpis,
    closeIssues,
    isLoading,
    error,
    refetch: fetchAnalytics,
  };
}
