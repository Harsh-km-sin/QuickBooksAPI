import { apiClient } from './core';
import type { QuickBooksToken } from '@/types';

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
