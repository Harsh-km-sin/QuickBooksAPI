import { apiClient } from './core';
import { buildListQuery } from './listQuery';
import type {
  ListQueryParams,
  PagedResult,
  ProductDto,
  CreateProductRequest,
  UpdateProductRequest,
  DeleteProductRequest,
} from '@/types';

export const productApi = {
  list: (params?: ListQueryParams) =>
    apiClient.get<PagedResult<ProductDto>>(`/api/product/list${buildListQuery(params)}`),
  sync: () => apiClient.get<number>('/api/product/sync'),
  create: (data: CreateProductRequest) =>
    apiClient.post<string>('/api/product/create', data),
  update: (data: UpdateProductRequest) =>
    apiClient.put<string>('/api/product/update', data),
  delete: (data: DeleteProductRequest) =>
    apiClient.delete<string>('/api/product/delete', data),
};
