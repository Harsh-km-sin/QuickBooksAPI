import { useState, useEffect, useCallback } from 'react';
import { journalEntryApi } from '@/api/client';
import type { QBOJournalEntryHeader } from '@/types';
import { toast } from 'sonner';

interface UseJournalEntriesReturn {
  entries: QBOJournalEntryHeader[];
  isLoading: boolean;
  isSyncing: boolean;
  error: string | null;
  refetch: () => Promise<void>;
  sync: () => Promise<void>;
}

export function useJournalEntries(): UseJournalEntriesReturn {
  const [entries, setEntries] = useState<QBOJournalEntryHeader[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSyncing, setIsSyncing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchEntries = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);
      const response = await journalEntryApi.list();
      
      if (response.success && response.data) {
        setEntries(response.data.items);
      } else {
        setError(response.message || 'Failed to fetch journal entries');
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'An unexpected error occurred';
      setError(message);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const sync = useCallback(async () => {
    try {
      setIsSyncing(true);
      const response = await journalEntryApi.sync();
      
      if (response.success) {
        toast.success('Sync completed', {
          description: `${response.data} journal entries synced from QuickBooks`,
        });
        await fetchEntries();
      } else {
        toast.error('Sync failed', {
          description: response.message || 'Failed to sync journal entries',
        });
      }
    } catch (err) {
      toast.error('Sync failed', {
        description: err instanceof Error ? err.message : 'An unexpected error occurred',
      });
    } finally {
      setIsSyncing(false);
    }
  }, [fetchEntries]);

  useEffect(() => {
    fetchEntries();
  }, [fetchEntries]);

  return {
    entries,
    isLoading,
    isSyncing,
    error,
    refetch: fetchEntries,
    sync,
  };
}
