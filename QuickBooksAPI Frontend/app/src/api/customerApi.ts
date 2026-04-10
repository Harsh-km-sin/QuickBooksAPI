import { apiClient } from './core';
import { buildListQuery } from './listQuery';
import type {
  ListQueryParams,
  PagedResult,
  Customer,
  CreateCustomerRequest,
  UpdateCustomerRequest,
  DeleteCustomerRequest,
} from '@/types';

export const customerApi = {
  list: (params?: ListQueryParams) =>
    apiClient.get<PagedResult<Customer>>(`/api/customer/list${buildListQuery(params)}`),
  getById: (id: string) =>
    apiClient.get<Customer>(`/api/customer/getById/${encodeURIComponent(id)}`),
  sync: () => apiClient.get<number>('/api/customer/sync'),
  create: (data: CreateCustomerRequest) =>
    apiClient.post<string>('/api/customer/create', data),
  update: (data: UpdateCustomerRequest) =>
    apiClient.put<string>('/api/customer/update', data),
  delete: (data: DeleteCustomerRequest) =>
    apiClient.delete<string>('/api/customer/delete', data),
};
