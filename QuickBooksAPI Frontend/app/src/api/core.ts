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

// Clear stored auth data (theme preference 'qb-connect-theme' is intentionally preserved)
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

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...((options.headers as Record<string, string>) || {}),
  };

  const token = getToken();
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const realmId = getRealmId();
  if (realmId) {
    headers['X-Realm-Id'] = realmId;
  }

  try {
    const response = await fetch(url, {
      ...options,
      headers,
    });

    const correlationId = response.headers.get('X-Correlation-Id') || undefined;

    let data: ApiResponse<T>;
    try {
      data = await response.json();
    } catch {
      data = {
        success: response.ok,
        message: response.ok ? 'Success' : 'Error',
        data: null as T,
        errors: null,
      };
    }

    if (!response.ok) {
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

    throw new ApiError(
      'Network error. Please check your connection and try again.',
      0,
      [error instanceof Error ? error.message : 'Unknown error']
    );
  }
}

export const apiClient = {
  get: <T>(endpoint: string, options?: RequestInit) =>
    request<T>(endpoint, { ...options, method: 'GET' }),

  post: <T>(endpoint: string, body: unknown, options?: RequestInit) =>
    request<T>(endpoint, {
      ...options,
      method: 'POST',
      body: JSON.stringify(body),
    }),

  put: <T>(endpoint: string, body: unknown, options?: RequestInit) =>
    request<T>(endpoint, {
      ...options,
      method: 'PUT',
      body: JSON.stringify(body),
    }),

  delete: <T>(endpoint: string, body?: unknown, options?: RequestInit) =>
    request<T>(endpoint, {
      ...options,
      method: 'DELETE',
      body: body ? JSON.stringify(body) : undefined,
    }),
};
