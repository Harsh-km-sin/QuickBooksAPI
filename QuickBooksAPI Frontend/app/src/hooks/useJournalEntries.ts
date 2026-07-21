import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { journalEntryApi } from '@/api/journalEntryApi';
import type { QBOJournalEntryHeader, ListQueryParams } from '@/types';
import { toast } from 'sonner';

const JOURNAL_ENTRIES_QUERY_KEY = ['journalEntries'] as const;

const defaultListParams: ListQueryParams = { page: 1, pageSize: 20 };

async function fetchJournalEntries(params: ListQueryParams) {
  const response = await journalEntryApi.list(params);
  if (!response.success || !response.data) {
    throw new Error(response.message || 'Failed to fetch journal entries');
  }
  return response.data;
}

interface UseJournalEntriesListReturn {
  entries: QBOJournalEntryHeader[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  isLoading: boolean;
  error: string | null;
}

/** Paged journal entry list query only — pair with `useJournalEntryMutations` for sync. */
export function useJournalEntriesList(listParams?: ListQueryParams): UseJournalEntriesListReturn {
  const params = { ...defaultListParams, ...listParams };

  const {
    data: pagedData,
    isLoading,
    error: queryError,
  } = useQuery({
    queryKey: [...JOURNAL_ENTRIES_QUERY_KEY, params.page, params.pageSize, params.search ?? '', params.sortBy ?? '', params.sortDir ?? ''],
    queryFn: () => fetchJournalEntries(params),
  });

  return {
    entries: pagedData?.items ?? [],
    totalCount: pagedData?.totalCount ?? 0,
    page: pagedData?.page ?? 1,
    pageSize: pagedData?.pageSize ?? defaultListParams.pageSize!,
    totalPages: pagedData?.totalPages ?? 0,
    hasNextPage: pagedData?.hasNextPage ?? false,
    hasPreviousPage: pagedData?.hasPreviousPage ?? false,
    isLoading,
    error: queryError ? (queryError instanceof Error ? queryError.message : 'An unexpected error occurred') : null,
  };
}

async function syncJournalEntries() {
  const response = await journalEntryApi.sync();
  if (!response.success) {
    throw new Error(response.message || 'Failed to sync journal entries');
  }
  return response.data;
}

interface UseJournalEntryMutationsReturn {
  isSyncing: boolean;
  sync: () => Promise<void>;
}

/** Sync only — no list query, safe to call from a page that doesn't own paging state. */
export function useJournalEntryMutations(): UseJournalEntryMutationsReturn {
  const queryClient = useQueryClient();

  const syncMutation = useMutation({
    mutationFn: syncJournalEntries,
    onSuccess: (count) => {
      toast.success('Sync completed', {
        description: `${count} journal entries synced from QuickBooks`,
      });
      queryClient.invalidateQueries({ queryKey: JOURNAL_ENTRIES_QUERY_KEY });
    },
    onError: (err) => {
      toast.error('Sync failed', {
        description: err instanceof Error ? err.message : 'An unexpected error occurred',
      });
    },
  });

  const sync = async () => {
    await syncMutation.mutateAsync();
  };

  return {
    isSyncing: syncMutation.isPending,
    sync,
  };
}
