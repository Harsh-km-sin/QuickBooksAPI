import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { billApi } from '@/api/billApi';
import type { QBOBillHeader, CreateBillRequest, UpdateBillRequest, DeleteBillRequest, ListQueryParams } from '@/types';
import { toast } from 'sonner';

const BILLS_QUERY_KEY = ['bills'] as const;

const defaultListParams: ListQueryParams = { page: 1, pageSize: 20 };

async function fetchBills(params: ListQueryParams) {
  const response = await billApi.list(params);
  if (!response.success || !response.data) {
    throw new Error(response.message || 'Failed to fetch bills');
  }
  return response.data;
}

interface UseBillsListReturn {
  bills: QBOBillHeader[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  isLoading: boolean;
  error: string | null;
}

/** Paged bill list query only — pair with `useBillMutations` for create/update/delete/sync. */
export function useBillsList(listParams?: ListQueryParams): UseBillsListReturn {
  const params = { ...defaultListParams, ...listParams };

  const {
    data: pagedData,
    isLoading,
    error: queryError,
  } = useQuery({
    queryKey: [...BILLS_QUERY_KEY, params.page, params.pageSize, params.search ?? '', params.sortBy ?? '', params.sortDir ?? ''],
    queryFn: () => fetchBills(params),
  });

  return {
    bills: pagedData?.items ?? [],
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

async function syncBills() {
  const response = await billApi.sync();
  if (!response.success) {
    throw new Error(response.message || 'Failed to sync bills');
  }
  return response.data;
}

interface UseBillMutationsReturn {
  isSyncing: boolean;
  sync: () => Promise<void>;
  getBillById: (id: string) => Promise<QBOBillHeader | null>;
  createBill: (data: CreateBillRequest) => Promise<boolean>;
  updateBill: (data: UpdateBillRequest) => Promise<boolean>;
  deleteBill: (data: DeleteBillRequest) => Promise<boolean>;
}

/** Sync/create/update/delete/getById — no list query, safe to call from a page that doesn't own paging state. */
export function useBillMutations(): UseBillMutationsReturn {
  const queryClient = useQueryClient();

  const syncMutation = useMutation({
    mutationFn: syncBills,
    onSuccess: (count) => {
      toast.success('Sync completed', {
        description: `${count} bills synced from QuickBooks`,
      });
      queryClient.invalidateQueries({ queryKey: BILLS_QUERY_KEY });
    },
    onError: (err) => {
      toast.error('Sync failed', {
        description: err instanceof Error ? err.message : 'An unexpected error occurred',
      });
    },
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateBillRequest) => billApi.create(data),
    onSuccess: (response) => {
      if (response.success) {
        toast.success('Bill created successfully');
        queryClient.invalidateQueries({ queryKey: BILLS_QUERY_KEY });
      } else {
        toast.error('Failed to create bill', { description: response.message || 'Please try again' });
      }
    },
    onError: (err) => {
      toast.error('Failed to create bill', {
        description: err instanceof Error ? err.message : 'An unexpected error occurred',
      });
    },
  });

  const updateMutation = useMutation({
    mutationFn: (data: UpdateBillRequest) => billApi.update(data),
    onSuccess: (response) => {
      if (response.success) {
        toast.success('Bill updated successfully');
        queryClient.invalidateQueries({ queryKey: BILLS_QUERY_KEY });
      } else {
        toast.error('Failed to update bill', { description: response.message || 'Please try again' });
      }
    },
    onError: (err) => {
      toast.error('Failed to update bill', {
        description: err instanceof Error ? err.message : 'An unexpected error occurred',
      });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (data: DeleteBillRequest) => billApi.delete(data),
    onSuccess: (response) => {
      if (response.success) {
        toast.success('Bill deleted successfully');
        queryClient.invalidateQueries({ queryKey: BILLS_QUERY_KEY });
      } else {
        toast.error('Failed to delete bill', { description: response.message || 'Please try again' });
      }
    },
    onError: (err) => {
      toast.error('Failed to delete bill', {
        description: err instanceof Error ? err.message : 'An unexpected error occurred',
      });
    },
  });

  const getBillById = async (id: string): Promise<QBOBillHeader | null> => {
    try {
      const response = await billApi.getById(id);
      return response.success && response.data ? response.data : null;
    } catch {
      return null;
    }
  };

  const sync = async () => {
    await syncMutation.mutateAsync();
  };

  const createBill = async (data: CreateBillRequest): Promise<boolean> => {
    try {
      const response = await createMutation.mutateAsync(data);
      return response.success;
    } catch {
      return false;
    }
  };

  const updateBill = async (data: UpdateBillRequest): Promise<boolean> => {
    try {
      const response = await updateMutation.mutateAsync(data);
      return response.success;
    } catch {
      return false;
    }
  };

  const deleteBill = async (data: DeleteBillRequest): Promise<boolean> => {
    try {
      const response = await deleteMutation.mutateAsync(data);
      return response.success;
    } catch {
      return false;
    }
  };

  return {
    isSyncing: syncMutation.isPending,
    sync,
    getBillById,
    createBill,
    updateBill,
    deleteBill,
  };
}
