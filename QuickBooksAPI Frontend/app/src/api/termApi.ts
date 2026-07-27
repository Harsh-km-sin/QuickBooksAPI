import { apiClient } from './core';
import type { QBOTerm } from '@/types/term';

export const termApi = {
  list: (activeOnly: boolean = true) =>
    apiClient.get<QBOTerm[]>(`/api/term/list?activeOnly=${activeOnly}`),
  sync: () =>
    apiClient.post<number>('/api/term/sync', {}),
};
