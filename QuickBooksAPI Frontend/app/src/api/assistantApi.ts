import { apiClient } from './core';
import type { CfoAssistantResponse } from '@/types';

export const assistantApi = {
  ask: (question: string) =>
    apiClient.post<CfoAssistantResponse>('/api/cfo-assistant/ask', { question }),
};
