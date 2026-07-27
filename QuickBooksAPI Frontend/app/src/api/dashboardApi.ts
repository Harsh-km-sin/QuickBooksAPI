import { apiClient } from './core';
import type { DashboardStats } from '@/types';

export const dashboardApi = {
  getStats: () => apiClient.get<DashboardStats>('/api/dashboard/stats'),
};
