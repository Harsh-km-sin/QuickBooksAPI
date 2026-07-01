/**
 * GL Review API — anomaly-detection runs, transactions, analytics, and review workflow.
 * Backend: QuickBooksAPI/Features/GlReview/* under `/api/gl-review`.
 */
import { apiClient, getToken, getRealmId, ApiError, API_BASE_URL } from './core';
import type {
  GlRun,
  GlRunSummary,
  GlTransaction,
  GlEntityRisk,
  GlPeriodMetric,
  GlAnomalyBreakdown,
  GlPagedResult,
  GlTransactionFilter,
  ApiResponse,
} from '@/types';

function transactionQuery(f?: GlTransactionFilter): string {
  if (!f) return '';
  const sp = new URLSearchParams();
  if (f.page != null) sp.set('page', String(f.page));
  if (f.pageSize != null) sp.set('pageSize', String(f.pageSize));
  if (f.riskTier) sp.set('riskTier', f.riskTier);
  if (f.accountName) sp.set('accountName', f.accountName);
  if (f.entityName) sp.set('entityName', f.entityName);
  if (f.dateFrom) sp.set('dateFrom', f.dateFrom);
  if (f.dateTo) sp.set('dateTo', f.dateTo);
  if (f.amountMin != null) sp.set('amountMin', String(f.amountMin));
  if (f.amountMax != null) sp.set('amountMax', String(f.amountMax));
  if (f.unreviewedOnly != null) sp.set('unreviewedOnly', String(f.unreviewedOnly));
  const q = sp.toString();
  return q ? `?${q}` : '';
}

const BASE = '/api/gl-review';

export const glReviewApi = {
  listRuns: () => apiClient.get<GlRun[]>(`${BASE}/runs`),
  getRun: (runId: number) => apiClient.get<GlRun>(`${BASE}/runs/${runId}`),
  getSummary: (runId: number) => apiClient.get<GlRunSummary>(`${BASE}/runs/${runId}/summary`),
  listTransactions: (runId: number, filter?: GlTransactionFilter) =>
    apiClient.get<GlPagedResult<GlTransaction>>(`${BASE}/runs/${runId}/transactions${transactionQuery(filter)}`),
  getTransaction: (runId: number, txId: number) =>
    apiClient.get<GlTransaction>(`${BASE}/runs/${runId}/transactions/${txId}`),
  getEntityRisk: (runId: number) =>
    apiClient.get<GlEntityRisk[]>(`${BASE}/runs/${runId}/analytics/entity-risk`),
  getPeriodMetrics: (runId: number) =>
    apiClient.get<GlPeriodMetric[]>(`${BASE}/runs/${runId}/analytics/period-metrics`),
  getAnomalyBreakdown: (runId: number) =>
    apiClient.get<GlAnomalyBreakdown[]>(`${BASE}/runs/${runId}/analytics/anomaly-breakdown`),
  markReviewed: (runId: number, txId: number, note?: string) =>
    apiClient.put<unknown>(`${BASE}/runs/${runId}/transactions/${txId}/review`, { note: note ?? null }),
  retryRun: (runId: number) => apiClient.post<{ runId: number; status: string }>(`${BASE}/runs/${runId}/retry`, {}),
  deleteRun: (runId: number) => apiClient.delete<unknown>(`${BASE}/runs/${runId}`),
  exportCsvUrl: (runId: number) => `${API_BASE_URL}${BASE}/runs/${runId}/export`,

  /**
   * Multipart upload — bypasses apiClient (which forces application/json) and lets the
   * browser set the multipart boundary. Mirrors the auth header logic in core.ts.
   */
  uploadFile: async (file: File): Promise<ApiResponse<{ runId: number; status: string }>> => {
    const form = new FormData();
    form.append('file', file);

    const headers: Record<string, string> = {};
    const token = getToken();
    if (token) headers['Authorization'] = `Bearer ${token}`;
    const realmId = getRealmId();
    if (realmId) headers['X-Realm-Id'] = realmId;

    let response: Response;
    try {
      response = await fetch(`${API_BASE_URL}${BASE}/upload`, { method: 'POST', headers, body: form });
    } catch (err) {
      throw new ApiError('Network error during upload. Please try again.', 0, [
        err instanceof Error ? err.message : 'Unknown error',
      ]);
    }

    let data: ApiResponse<{ runId: number; status: string }>;
    try {
      data = await response.json();
    } catch {
      data = { success: response.ok, message: response.ok ? 'Success' : 'Upload failed', data: null, errors: null };
    }
    if (!response.ok) {
      throw new ApiError(data.message || 'Upload failed', response.status, data.errors || []);
    }
    return data;
  },
};
