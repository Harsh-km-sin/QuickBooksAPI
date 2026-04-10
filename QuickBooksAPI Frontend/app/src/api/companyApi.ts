import { apiClient } from './core';
import type { ConnectedCompany } from '@/types';

export const companyApi = {
  fullSync: () => apiClient.post<string>('/api/company/sync/full', {}),
  syncStatus: () =>
    apiClient.get<{ companyId: string; status: string; lastRun: string | null; error: string | null }>(
      '/api/company/sync/status'
    ),
  getConnectedCompanies: () => apiClient.get<ConnectedCompany[]>('/api/company/connected-companies'),
  disconnect: (realmId: string) => apiClient.post<string>('/api/company/disconnect', { realmId }),
};
