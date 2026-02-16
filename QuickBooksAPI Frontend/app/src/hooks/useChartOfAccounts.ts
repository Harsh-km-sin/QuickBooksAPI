import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { chartOfAccountsApi } from '@/api/client';
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

async function syncChartOfAccounts() {
  const response = await chartOfAccountsApi.sync();
  if (!response.success) {
    throw new Error(response.message || 'Failed to sync chart of accounts');
  }
  return response.data;
}

interface UseChartOfAccountsOptions {
  listParams?: ListQueryParams;
}

interface UseChartOfAccountsReturn {
  accounts: ChartOfAccounts[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  isLoading: boolean;
  isSyncing: boolean;
  error: string | null;
  refetch: () => Promise<void>;
  sync: () => Promise<void>;
}

export function useChartOfAccounts(options?: UseChartOfAccountsOptions): UseChartOfAccountsReturn {
  const queryClient = useQueryClient();
  const listParams = { ...defaultListParams, ...options?.listParams };

  const {
    data: pagedData,
    isLoading,
    error: queryError,
    refetch: queryRefetch,
  } = useQuery({
    queryKey: [...CHART_OF_ACCOUNTS_QUERY_KEY, listParams.page, listParams.pageSize, listParams.search ?? ''],
    queryFn: () => fetchChartOfAccounts(listParams),
  });

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

  const refetch = async () => {
    await queryRefetch();
  };

  const sync = async () => {
    await syncMutation.mutateAsync();
  };

  const accounts = pagedData?.items ?? [];
  const totalCount = pagedData?.totalCount ?? 0;
  const page = pagedData?.page ?? 1;
  const pageSize = pagedData?.pageSize ?? defaultListParams.pageSize!;
  const totalPages = pagedData?.totalPages ?? 0;
  const hasNextPage = pagedData?.hasNextPage ?? false;
  const hasPreviousPage = pagedData?.hasPreviousPage ?? false;

  return {
    accounts,
    totalCount,
    page,
    pageSize,
    totalPages,
    hasNextPage,
    hasPreviousPage,
    isLoading,
    isSyncing: syncMutation.isPending,
    error: queryError ? (queryError instanceof Error ? queryError.message : 'An unexpected error occurred') : null,
    refetch,
    sync,
  };
}
