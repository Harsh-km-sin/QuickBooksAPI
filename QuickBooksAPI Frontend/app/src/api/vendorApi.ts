import { apiClient } from './core';
import { buildListQuery } from './listQuery';
import type {
  ListQueryParams,
  PagedResult,
  Vendor,
  CreateVendorRequest,
  UpdateVendorRequest,
  SoftDeleteVendorRequest,
} from '@/types';

export const vendorApi = {
  list: (params?: ListQueryParams) =>
    apiClient.get<PagedResult<Vendor>>(`/api/vendor/list${buildListQuery(params)}`),
  sync: () => apiClient.get<number>('/api/vendor/sync'),
  create: (data: CreateVendorRequest) =>
    apiClient.post<string>('/api/vendor/create', data),
  update: (data: UpdateVendorRequest) =>
    apiClient.put<string>('/api/vendor/update', data),
  softDelete: (data: SoftDeleteVendorRequest) =>
    apiClient.delete<string>('/api/vendor/softDelete', data),
};
