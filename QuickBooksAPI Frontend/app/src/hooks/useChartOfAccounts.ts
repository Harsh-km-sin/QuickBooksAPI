import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { chartOfAccountsApi } from '@/api/chartOfAccountsApi';
import type { ChartOfAccounts, ListQueryParams } from '@/types';
import { toast } from 'sonner';

const CHART_OF_ACCOUNTS_QUERY_KEY = ['chartOfAccounts'] as const;

const defaultListParams: ListQueryParams = { page: 1, pageSize: 20 };

async function fetchChartOfAccounts(params: ListQueryParams) {
  const response = await chartOfAccountsApi.list(params);
  if (!response.success || !response.data) {
    throw new Error(response.message || 'Failed to fetch chart of accounts');
  }
  return response.data;
}

interface UseChartOfAccountsListReturn {
  accounts: ChartOfAccounts[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  isLoading: boolean;
  error: string | null;
}

/** Paged chart-of-accounts list query only — pair with `useChartOfAccountsMutations` for sync. */
export function useChartOfAccountsList(listParams?: ListQueryParams): UseChartOfAccountsListReturn {
  const params = { ...defaultListParams, ...listParams };

  const {
    data: pagedData,
    isLoading,
    error: queryError,
  } = useQuery({
    queryKey: [...CHART_OF_ACCOUNTS_QUERY_KEY, params.page, params.pageSize, params.search ?? '', params.sortBy ?? '', params.sortDir ?? ''],
    queryFn: () => fetchChartOfAccounts(params),
  });

  return {
    accounts: pagedData?.items ?? [],
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

async function syncChartOfAccounts() {
  const response = await chartOfAccountsApi.sync();
  if (!response.success) {
    throw new Error(response.message || 'Failed to sync chart of accounts');
  }
  return response.data;
}

interface UseChartOfAccountsMutationsReturn {
  isSyncing: boolean;
  sync: () => Promise<void>;
}

/** Sync only — no list query, safe to call from a page that doesn't own paging state. */
export function useChartOfAccountsMutations(): UseChartOfAccountsMutationsReturn {
  const queryClient = useQueryClient();

  const syncMutation = useMutation({
    mutationFn: syncChartOfAccounts,
    onSuccess: (count) => {
      toast.success('Sync completed', {
        description: `${count} accounts synced from QuickBooks`,
      });
      queryClient.invalidateQueries({ queryKey: CHART_OF_ACCOUNTS_QUERY_KEY });
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
