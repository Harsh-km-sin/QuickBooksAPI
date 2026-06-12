import { apiClient } from './core';
import type { ApiResponse } from '@/types';

export interface UserProfile {
  id: number;
  firstName: string;
  lastName: string;
  username: string;
  email: string;
}

export const settingsApi = {
  getProfile: (): Promise<ApiResponse<UserProfile>> =>
    apiClient.get<UserProfile>('/api/user/profile'),

  updateProfile: (data: {
    firstName: string;
    lastName: string;
    username: string;
  }): Promise<ApiResponse<null>> =>
    apiClient.put<null>('/api/user/profile', data),

  changePassword: (data: {
    currentPassword: string;
    newPassword: string;
  }): Promise<ApiResponse<null>> =>
    apiClient.put<null>('/api/user/password', data),
};
