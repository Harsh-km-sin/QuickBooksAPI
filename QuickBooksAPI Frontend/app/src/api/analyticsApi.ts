/**
 * CFO / dashboard analytics endpoints — isolated module for AI-friendly navigation (import from `@/api/client` re-exports).
 */
import { apiClient } from './core';
import type {
  CashRunwayResult,
  VendorSpend,
  VendorSpendSummary,
  CustomerProfitability,
  RevenueExpensesMonthly,
  Anomaly,
  KpiSnapshot,
  ForecastScenario,
  ForecastDetail,
  CreateForecastRequest,
  CloseIssue,
  Entity,
  ConsolidatedPnlRow,
} from '@/types';

function analyticsQuery(params?: {
  period?: number;
  limit?: number;
  from?: string;
  to?: string;
  top?: number;
  since?: string;
  names?: string;
}): string {
  if (!params) return '';
  const search = new URLSearchParams();
  if (params.period != null) search.set('period', String(params.period));
  if (params.limit != null) search.set('limit', String(params.limit));
  if (params.from) search.set('from', params.from);
  if (params.to) search.set('to', params.to);
  if (params.top != null) search.set('top', String(params.top));
  if (params.since) search.set('since', params.since);
  if (params.names) search.set('names', params.names);
  const q = search.toString();
  return q ? `?${q}` : '';
}

export const analyticsApi = {
  getCashRunway: () => apiClient.get<CashRunwayResult>('/api/analytics/cash-runway'),
  getVendorSpendTop: (period = 30, limit = 10) =>
    apiClient.get<VendorSpend[]>(`/api/analytics/vendor-spend/top${analyticsQuery({ period, limit })}`),
  getVendorSpendSummary: (from?: string, to?: string) =>
    apiClient.get<VendorSpendSummary>(`/api/analytics/vendor-spend/summary${analyticsQuery({ from, to })}`),
  getCustomerProfitability: (params?: { from?: string; to?: string; top?: number }) =>
    apiClient.get<CustomerProfitability[]>(`/api/analytics/customer-profitability${analyticsQuery(params)}`),
  getRevenueExpenses: (params?: { from?: string; to?: string }) =>
    apiClient.get<RevenueExpensesMonthly[]>(`/api/analytics/revenue-expenses${analyticsQuery(params)}`),
  getAnomalies: (since?: string) =>
    apiClient.get<Anomaly[]>(`/api/analytics/anomalies${analyticsQuery({ since })}`),
  getKpis: (params?: { from?: string; to?: string; names?: string }) =>
    apiClient.get<KpiSnapshot[]>(`/api/analytics/kpis${analyticsQuery(params)}`),
  createForecast: (body: CreateForecastRequest) =>
    apiClient.post<ForecastScenario>('/api/analytics/forecast', body),
  getForecast: (id: number) => apiClient.get<ForecastDetail>(`/api/analytics/forecast/${id}`),
  getCloseIssues: (params?: { since?: string; severity?: string; unresolvedOnly?: boolean }) => {
    const sp = new URLSearchParams();
    if (params?.since) sp.set('since', params.since);
    if (params?.severity) sp.set('severity', params.severity);
    if (params?.unresolvedOnly !== undefined) sp.set('unresolvedOnly', String(params.unresolvedOnly));
    const q = sp.toString();
    return apiClient.get<CloseIssue[]>(`/api/analytics/close-issues${q ? `?${q}` : ''}`);
  },
  resolveCloseIssue: (id: number) =>
    apiClient.post<unknown>(`/api/analytics/close-issues/${id}/resolve`, {}),
  getEntities: () => apiClient.get<Entity[]>('/api/analytics/entities'),
  getConsolidatedPnl: (entityId: number, from?: string, to?: string) => {
    const sp = new URLSearchParams();
    sp.set('entityId', String(entityId));
    if (from) sp.set('from', from);
    if (to) sp.set('to', to);
    return apiClient.get<ConsolidatedPnlRow[]>(`/api/analytics/consolidated-pnl?${sp.toString()}`);
  },
};
