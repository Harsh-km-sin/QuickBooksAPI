import { apiClient } from './core';
import { buildListQuery } from './listQuery';
import type {
  ListQueryParams,
  PagedResult,
  QBOInvoiceHeader,
  CreateInvoiceRequest,
  UpdateInvoiceRequest,
  DeleteInvoiceRequest,
  VoidInvoiceRequest,
} from '@/types';

export const invoiceApi = {
  list: (params?: ListQueryParams) =>
    apiClient.get<PagedResult<QBOInvoiceHeader>>(`/api/invoice/list${buildListQuery(params)}`),
  sync: () => apiClient.get<number>('/api/invoice/sync'),
  create: (data: CreateInvoiceRequest) =>
    apiClient.post<string>('/api/invoice/create', data),
  update: (data: UpdateInvoiceRequest) =>
    apiClient.put<string>('/api/invoice/update', data),
  delete: (data: DeleteInvoiceRequest) =>
    apiClient.delete<string>('/api/invoice/delete', data),
  void: (data: VoidInvoiceRequest) =>
    apiClient.post<string>('/api/invoice/void', data),
};
