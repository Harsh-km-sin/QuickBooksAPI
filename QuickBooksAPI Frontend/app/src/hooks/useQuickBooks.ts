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
      
      if (response.success && response.data) {
        // Redirect to QuickBooks OAuth URL
        window.location.href = response.data;
      } else {
        toast.error('Failed to get QuickBooks connection URL', {
          description: response.message || 'Please try again',
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
