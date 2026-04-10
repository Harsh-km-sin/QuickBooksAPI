import { apiClient } from './core';
import { buildListQuery } from './listQuery';
import type {
  ListQueryParams,
  PagedResult,
  QBOBillHeader,
  CreateBillRequest,
  UpdateBillRequest,
  DeleteBillRequest,
} from '@/types';

export const billApi = {
  list: (params?: ListQueryParams) =>
    apiClient.get<PagedResult<QBOBillHeader>>(`/api/bill/list${buildListQuery(params)}`),
  getById: (id: string) =>
    apiClient.get<QBOBillHeader>(`/api/bill/getById?id=${encodeURIComponent(id)}`),
  sync: () => apiClient.get<number>('/api/bill/sync'),
  create: (data: CreateBillRequest) =>
    apiClient.post<string>('/api/bill/create', data),
  update: (data: UpdateBillRequest) =>
    apiClient.put<string>('/api/bill/update', data),
  delete: (data: DeleteBillRequest) =>
    apiClient.delete<string>('/api/bill/delete', data),
};
