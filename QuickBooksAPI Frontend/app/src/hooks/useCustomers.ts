import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { customerApi } from '@/api/customerApi';
import type { Customer, CreateCustomerRequest, UpdateCustomerRequest, DeleteCustomerRequest, ListQueryParams } from '@/types';
import { toast } from 'sonner';

const CUSTOMERS_QUERY_KEY = ['customers'] as const;

const defaultListParams: ListQueryParams = { page: 1, pageSize: 20 };

async function fetchCustomers(params: ListQueryParams) {
  const response = await customerApi.list(params);
  if (!response.success || !response.data) {
    throw new Error(response.message || 'Failed to fetch customers');
  }
  return response.data;
}

interface UseCustomersListReturn {
  customers: Customer[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  isLoading: boolean;
  error: string | null;
}

/** Paged customer list query only — pair with `useCustomerMutations` for create/update/delete/sync. */
export function useCustomersList(listParams?: ListQueryParams): UseCustomersListReturn {
  const params = { ...defaultListParams, ...listParams };

  const {
    data: pagedData,
    isLoading,
    error: queryError,
  } = useQuery({
    queryKey: [...CUSTOMERS_QUERY_KEY, params.page, params.pageSize, params.search ?? '', params.activeFilter ?? 'active', params.sortBy ?? '', params.sortDir ?? ''],
    queryFn: () => fetchCustomers(params),
  });

  return {
    customers: pagedData?.items ?? [],
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

async function syncCustomers() {
  const response = await customerApi.sync();
  if (!response.success) {
    throw new Error(response.message || 'Failed to sync customers');
  }
  return response.data;
}

interface UseCustomerMutationsReturn {
  isSyncing: boolean;
  sync: () => Promise<void>;
  getCustomerById: (id: string) => Promise<Customer | null>;
  createCustomer: (data: CreateCustomerRequest) => Promise<boolean>;
  updateCustomer: (data: UpdateCustomerRequest) => Promise<boolean>;
  deleteCustomer: (data: DeleteCustomerRequest) => Promise<boolean>;
}

/** Sync/create/update/delete/getById — no list query, safe to call from a page that doesn't own paging state. */
export function useCustomerMutations(): UseCustomerMutationsReturn {
  const queryClient = useQueryClient();

  const syncMutation = useMutation({
    mutationFn: syncCustomers,
    onSuccess: (count) => {
      toast.success('Sync completed', {
        description: `${count} customers synced from QuickBooks`,
      });
      queryClient.invalidateQueries({ queryKey: CUSTOMERS_QUERY_KEY });
    },
    onError: (err) => {
      toast.error('Sync failed', {
        description: err instanceof Error ? err.message : 'An unexpected error occurred',
      });
    },
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateCustomerRequest) => customerApi.create(data),
    onSuccess: (response) => {
      if (response.success) {
        toast.success('Customer created successfully');
        queryClient.invalidateQueries({ queryKey: CUSTOMERS_QUERY_KEY });
      } else {
        toast.error('Failed to create customer', {
          description: response.message || 'Please try again',
        });
      }
    },
    onError: (err) => {
      toast.error('Failed to create customer', {
        description: err instanceof Error ? err.message : 'An unexpected error occurred',
      });
    },
  });

  const updateMutation = useMutation({
    mutationFn: (data: UpdateCustomerRequest) => customerApi.update(data),
    onSuccess: (response) => {
      if (response.success) {
        toast.success('Customer updated successfully');
        queryClient.invalidateQueries({ queryKey: CUSTOMERS_QUERY_KEY });
      } else {
        toast.error('Failed to update customer', {
          description: response.message || 'Please try again',
        });
      }
    },
    onError: (err) => {
      toast.error('Failed to update customer', {
        description: err instanceof Error ? err.message : 'An unexpected error occurred',
      });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (data: DeleteCustomerRequest) => customerApi.delete(data),
    onSuccess: (response) => {
      if (response.success) {
        toast.success('Customer deleted successfully');
        queryClient.invalidateQueries({ queryKey: CUSTOMERS_QUERY_KEY });
      } else {
        toast.error('Failed to delete customer', {
          description: response.message || 'Please try again',
        });
      }
    },
    onError: (err) => {
      toast.error('Failed to delete customer', {
        description: err instanceof Error ? err.message : 'An unexpected error occurred',
      });
    },
  });

  const getCustomerById = async (id: string): Promise<Customer | null> => {
    try {
      const response = await customerApi.getById(id);
      return response.success && response.data ? response.data : null;
    } catch {
      return null;
    }
  };

  const sync = async () => {
    await syncMutation.mutateAsync();
  };

  const createCustomer = async (data: CreateCustomerRequest): Promise<boolean> => {
    try {
      const response = await createMutation.mutateAsync(data);
      return response.success;
    } catch {
      return false;
    }
  };

  const updateCustomer = async (data: UpdateCustomerRequest): Promise<boolean> => {
    try {
      const response = await updateMutation.mutateAsync(data);
      return response.success;
    } catch {
      return false;
    }
  };

  const deleteCustomer = async (data: DeleteCustomerRequest): Promise<boolean> => {
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
    getCustomerById,
    createCustomer,
    updateCustomer,
    deleteCustomer,
  };
}
