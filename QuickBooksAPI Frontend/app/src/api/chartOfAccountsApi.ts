import { apiClient } from './core';
import { buildListQuery } from './listQuery';
import type { ListQueryParams, PagedResult, ChartOfAccounts } from '@/types';

export const chartOfAccountsApi = {
  list: (params?: ListQueryParams) =>
    apiClient.get<PagedResult<ChartOfAccounts>>(`/api/chartofaccounts/list${buildListQuery(params)}`),
  sync: () => apiClient.get<number>('/api/chartofaccounts/sync'),
};
