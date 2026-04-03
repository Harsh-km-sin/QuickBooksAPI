/**
 * API barrel: shared HTTP core + feature modules. Prefer importing feature modules (e.g. `@/api/analyticsApi`) for large changes.
 */
export * from './core';
export { analyticsApi } from './analyticsApi';

import { apiClient } from './core';
import type {
  ListQueryParams,
  PagedResult,
  Customer,
  ConnectedCompany,
  CreateCustomerRequest,
  UpdateCustomerRequest,
  DeleteCustomerRequest,
  Products,
  CreateProductRequest,
  UpdateProductRequest,
  DeleteProductRequest,
  Vendor,
  CreateVendorRequest,
  UpdateVendorRequest,
  SoftDeleteVendorRequest,
  QBOBillHeader,
  CreateBillRequest,
  UpdateBillRequest,
  DeleteBillRequest,
  QBOInvoiceHeader,
  CreateInvoiceRequest,
  UpdateInvoiceRequest,
  DeleteInvoiceRequest,
  VoidInvoiceRequest,
  ChartOfAccounts,
  QBOJournalEntryHeader,
  QuickBooksToken,
  CfoAssistantResponse,
} from '@/types';

export const authApi = {
  login: (email: string, password: string) =>
    apiClient.post<string>('/api/auth/login', { email, password }),

  signUp: (data: {
    firstName: string;
    lastName: string;
    username: string;
    email: string;
    password: string;
  }) => apiClient.post<number>('/api/auth/SignUp', data),

  getOAuthUrl: () => apiClient.get<string>('/api/auth/oAuth'),

  handleCallback: (code: string, state: string, realmId: string) =>
    apiClient.get<QuickBooksToken>(
      `/api/auth/callback?code=${encodeURIComponent(code)}&state=${encodeURIComponent(
        state
      )}&realmId=${encodeURIComponent(realmId)}`
    ),

  logout: () => apiClient.post<string>('/api/auth/logout', {}),
};

function buildListQuery(params?: ListQueryParams): string {
  if (!params) return '';
  const search = new URLSearchParams();
  if (params.page != null) search.set('page', String(params.page));
  if (params.pageSize != null) search.set('pageSize', String(params.pageSize));
  if (params.search) search.set('search', params.search);
  if (params.activeFilter && params.activeFilter !== 'all') search.set('activeFilter', params.activeFilter);
  const q = search.toString();
  return q ? `?${q}` : '';
}

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

export const productApi = {
  list: (params?: ListQueryParams) =>
    apiClient.get<PagedResult<Products>>(`/api/product/list${buildListQuery(params)}`),
  sync: () => apiClient.get<number>('/api/product/sync'),
  create: (data: CreateProductRequest) =>
    apiClient.post<string>('/api/product/create', data),
  update: (data: UpdateProductRequest) =>
    apiClient.put<string>('/api/product/update', data),
  delete: (data: DeleteProductRequest) =>
    apiClient.delete<string>('/api/product/delete', data),
};

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

export const chartOfAccountsApi = {
  list: (params?: ListQueryParams) =>
    apiClient.get<PagedResult<ChartOfAccounts>>(`/api/chartofaccounts/list${buildListQuery(params)}`),
  sync: () => apiClient.get<number>('/api/chartofaccounts/sync'),
};

export const companyApi = {
  fullSync: () => apiClient.post<string>('/api/company/sync/full', {}),
  syncStatus: () =>
    apiClient.get<{ companyId: string; status: string; lastRun: string | null; error: string | null }>(
      '/api/company/sync/status'
    ),
  getConnectedCompanies: () => apiClient.get<ConnectedCompany[]>('/api/company/connected-companies'),
  disconnect: (realmId: string) => apiClient.post<string>('/api/company/disconnect', { realmId }),
};

export const journalEntryApi = {
  list: (params?: ListQueryParams) =>
    apiClient.get<PagedResult<QBOJournalEntryHeader>>(`/api/journalentry/list${buildListQuery(params)}`),
  sync: () => apiClient.get<number>('/api/journalentry/sync'),
};

export const assistantApi = {
  ask: (question: string) =>
    apiClient.post<CfoAssistantResponse>('/api/cfo-assistant/ask', { question }),
};
