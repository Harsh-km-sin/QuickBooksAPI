import { apiClient } from './core';
import { buildListQuery } from './listQuery';
import type { ListQueryParams, PagedResult, QBOJournalEntryHeader } from '@/types';

export const journalEntryApi = {
  list: (params?: ListQueryParams) =>
    apiClient.get<PagedResult<QBOJournalEntryHeader>>(`/api/journalentry/list${buildListQuery(params)}`),
  sync: () => apiClient.get<number>('/api/journalentry/sync'),
};
