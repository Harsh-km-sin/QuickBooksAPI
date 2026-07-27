import { apiClient } from './core';
import type { ReportTree, ReportPeriod, ReportType } from '@/types';

/**
 * All dates here are exchanged as yyyy-MM-dd strings. Convert with `toApiDate` from
 * `@/components/ui/date-picker` — never `toISOString()`, which shifts the day for anyone east of UTC.
 */
export const reportApi = {
  /** Reads stored data only — no QuickBooks call. */
  profitAndLoss: (params: { startDate?: string; endDate?: string; useFiscalYear?: boolean; accountingMethod?: string }) => {
    const query = new URLSearchParams();
    if (params.startDate) query.set('startDate', params.startDate);
    if (params.endDate) query.set('endDate', params.endDate);
    if (params.useFiscalYear) query.set('useFiscalYear', 'true');
    if (params.accountingMethod) query.set('accountingMethod', params.accountingMethod);
    return apiClient.get<ReportTree>(`/api/report/profit-and-loss?${query.toString()}`);
  },

  /** Point-in-time: returns the latest stored month-end at or before `asOfDate`. */
  balanceSheet: (asOfDate?: string, accountingMethod?: string) => {
    const query = new URLSearchParams();
    if (asOfDate) query.set('asOfDate', asOfDate);
    if (accountingMethod) query.set('accountingMethod', accountingMethod);
    const queryString = query.toString();
    return apiClient.get<ReportTree>(`/api/report/balance-sheet${queryString ? `?${queryString}` : ''}`);
  },

  periods: (reportType: ReportType) =>
    apiClient.get<ReportPeriod[]>(`/api/report/periods?reportType=${encodeURIComponent(reportType)}`),

  /** The only endpoint here that calls QuickBooks. Backs the "Generate Reports" button. */
  sync: () => apiClient.get<number>('/api/report/sync'),
};
