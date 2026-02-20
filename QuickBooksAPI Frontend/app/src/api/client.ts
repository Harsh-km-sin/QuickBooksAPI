import type { ApiResponse } from '@/types';

// API Configuration
const API_BASE_URL = import.meta.env.VITE_API_URL || 'https://localhost:7135';

// Custom error class for API errors
export class ApiError extends Error {
  status: number;
  errors?: string[];
  correlationId?: string;
  
  constructor(
    message: string,
    status: number,
    errors?: string[],
    correlationId?: string
  ) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.errors = errors;
    this.correlationId = correlationId;
  }
}

// Get stored token
export function getToken(): string | null {
  return sessionStorage.getItem('jwt_token');
}

// Get stored realm ID
export function getRealmId(): string | null {
  return localStorage.getItem('realm_id');
}

// Set stored token
export function setToken(token: string): void {
  sessionStorage.setItem('jwt_token', token);
}

// Set stored realm ID
export function setRealmId(realmId: string): void {
  localStorage.setItem('realm_id', realmId);
}

// Clear stored auth data
export function clearAuth(): void {
  sessionStorage.removeItem('jwt_token');
  localStorage.removeItem('realm_id');
}

// Parse JWT token to get claims
export function parseJwt(token: string): { UserId: string; Name: string; RealmIds: string[] } | null {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    return JSON.parse(jsonPayload);
  } catch {
    return null;
  }
}

// Main API request function
async function request<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<ApiResponse<T>> {
  const url = `${API_BASE_URL}${endpoint}`;
  
  // Prepare headers
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...((options.headers as Record<string, string>) || {}),
  };

  // Add authorization header if token exists
  const token = getToken();
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  // Add realm ID header if exists
  const realmId = getRealmId();
  if (realmId) {
    headers['X-Realm-Id'] = realmId;
  }

  try {
    const response = await fetch(url, {
      ...options,
      headers,
    });

    // Get correlation ID from response headers
    const correlationId = response.headers.get('X-Correlation-Id') || undefined;

    // Parse response body
    let data: ApiResponse<T>;
    try {
      data = await response.json();
    } catch {
      // If response is not JSON, create a generic response
      data = {
        success: response.ok,
        message: response.ok ? 'Success' : 'Error',
        data: null as T,
        errors: null,
      };
    }

    // Handle HTTP errors
    if (!response.ok) {
      // Handle specific status codes
      if (response.status === 401) {
        clearAuth();
        window.location.href = '/login';
        throw new ApiError(
          'Session expired. Please log in again.',
          401,
          data.errors || [],
          correlationId
        );
      }

      if (response.status === 403) {
        throw new ApiError(
          'Access denied. You do not have permission to perform this action.',
          403,
          data.errors || [],
          correlationId
        );
      }

      if (response.status === 429) {
        throw new ApiError(
          'Too many requests. Please try again later.',
          429,
          data.errors || [],
          correlationId
        );
      }

      if (response.status >= 500) {
        throw new ApiError(
          data.message || 'An unexpected error occurred. Please try again later.',
          response.status,
          data.errors || [],
          correlationId
        );
      }

      throw new ApiError(
        data.message || 'Request failed',
        response.status,
        data.errors || [],
        correlationId
      );
    }

    return data;
  } catch (error) {
    if (error instanceof ApiError) {
      throw error;
    }
    
    // Network or other errors
    throw new ApiError(
      'Network error. Please check your connection and try again.',
      0,
      [error instanceof Error ? error.message : 'Unknown error']
    );
  }
}

// API client methods
export const apiClient = {
  // GET request
  get: <T>(endpoint: string, options?: RequestInit) =>
    request<T>(endpoint, { ...options, method: 'GET' }),

  // POST request
  post: <T>(endpoint: string, body: unknown, options?: RequestInit) =>
    request<T>(endpoint, {
      ...options,
      method: 'POST',
      body: JSON.stringify(body),
    }),

  // PUT request
  put: <T>(endpoint: string, body: unknown, options?: RequestInit) =>
    request<T>(endpoint, {
      ...options,
      method: 'PUT',
      body: JSON.stringify(body),
    }),

  // DELETE request
  delete: <T>(endpoint: string, body?: unknown, options?: RequestInit) =>
    request<T>(endpoint, {
      ...options,
      method: 'DELETE',
      body: body ? JSON.stringify(body) : undefined,
    }),
};

// Auth API
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

  getConnectedCompanies: () =>
    apiClient.get<ConnectedCompany[]>('/api/auth/connected-companies'),

  disconnect: (realmId: string) =>
    apiClient.post<string>('/api/auth/disconnect', { realmId }),

  logout: () =>
    apiClient.post<string>('/api/auth/logout', {}),
};

function buildListQuery(params?: ListQueryParams): string {
  if (!params) return '';
  const search = new URLSearchParams();
  if (params.page != null) search.set('page', String(params.page));
  if (params.pageSize != null) search.set('pageSize', String(params.pageSize));
  if (params.search) search.set('search', params.search);
  const q = search.toString();
  return q ? `?${q}` : '';
}

// Customer API
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

// Product API
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

// Vendor API
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

// Bill API
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

// Invoice API
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

// Chart of Accounts API
export const chartOfAccountsApi = {
  list: (params?: ListQueryParams) =>
    apiClient.get<PagedResult<ChartOfAccounts>>(`/api/chartofaccounts/list${buildListQuery(params)}`),
  sync: () => apiClient.get<number>('/api/chartofaccounts/sync'),
};

// Journal Entry API
export const journalEntryApi = {
  list: (params?: ListQueryParams) =>
    apiClient.get<PagedResult<QBOJournalEntryHeader>>(`/api/journalentry/list${buildListQuery(params)}`),
  sync: () => apiClient.get<number>('/api/journalentry/sync'),
};

// Import types
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
} from '@/types';
