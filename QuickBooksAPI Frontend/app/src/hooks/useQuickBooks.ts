import { useState, useCallback } from 'react';
import { authApi } from '@/api/client';
import { toast } from 'sonner';

interface UseQuickBooksReturn {
  isConnecting: boolean;
  connect: () => Promise<void>;
}

export function useQuickBooks(): UseQuickBooksReturn {
  const [isConnecting, setIsConnecting] = useState(false);

  const connect = useCallback(async () => {
    try {
      setIsConnecting(true);
      const response = await authApi.getOAuthUrl();
      // Backend may return ApiResponse<string> (response.data) or raw URL string
      const url = typeof response === 'string' ? response : response?.data ?? null;
      if (url) {
        window.location.href = url;
      } else {
        toast.error('Failed to get QuickBooks connection URL', {
          description: (typeof response === 'object' && response?.message) ? response.message : 'Please try again',
        });
      }
    } catch (err) {
      toast.error('Failed to connect to QuickBooks', {
        description: err instanceof Error ? err.message : 'An unexpected error occurred',
      });
    } finally {
      setIsConnecting(false);
    }
  }, []);

  return {
    isConnecting,
    connect,
  };
}
