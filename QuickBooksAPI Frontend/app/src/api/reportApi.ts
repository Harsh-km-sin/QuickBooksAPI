import { apiClient } from './core';
import type { ReportTree, ReportPeriod, ReportType } from '@/types';

/** QBO dates are exchanged as yyyy-MM-dd; sending an ISO timestamp would drift across time zones. */
export function toApiDate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export const reportApi = {
  /** Reads stored data only — no QuickBooks call. */
  profitAndLoss: (params: { startDate?: string; endDate?: string; useFiscalYear?: boolean }) => {
    const query = new URLSearchParams();
    if (params.startDate) query.set('startDate', params.startDate);
    if (params.endDate) query.set('endDate', params.endDate);
    if (params.useFiscalYear) query.set('useFiscalYear', 'true');
    return apiClient.get<ReportTree>(`/api/report/profit-and-loss?${query.toString()}`);
  },

  /** Point-in-time: returns the latest stored month-end at or before `asOfDate`. */
  balanceSheet: (asOfDate?: string) => {
    const query = asOfDate ? `?asOfDate=${encodeURIComponent(asOfDate)}` : '';
    return apiClient.get<ReportTree>(`/api/report/balance-sheet${query}`);
  },

  periods: (reportType: ReportType) =>
    apiClient.get<ReportPeriod[]>(`/api/report/periods?reportType=${encodeURIComponent(reportType)}`),

  /** The only endpoint here that calls QuickBooks. Backs the "Generate Reports" button. */
  sync: () => apiClient.get<number>('/api/report/sync'),
};
