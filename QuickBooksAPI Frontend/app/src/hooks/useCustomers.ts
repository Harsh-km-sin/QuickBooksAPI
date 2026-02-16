import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { customerApi } from '@/api/client';
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

async function syncCustomers() {
  const response = await customerApi.sync();
  if (!response.success) {
    throw new Error(response.message || 'Failed to sync customers');
  }
  return response.data;
}

interface UseCustomersOptions {
  listParams?: ListQueryParams;
}

interface UseCustomersReturn {
  customers: Customer[];
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
  createCustomer: (data: CreateCustomerRequest) => Promise<boolean>;
  updateCustomer: (data: UpdateCustomerRequest) => Promise<boolean>;
  deleteCustomer: (data: DeleteCustomerRequest) => Promise<boolean>;
}

export function useCustomers(options?: UseCustomersOptions): UseCustomersReturn {
  const queryClient = useQueryClient();
  const listParams = { ...defaultListParams, ...options?.listParams };

  const {
    data: pagedData,
    isLoading,
    error: queryError,
    refetch: queryRefetch,
  } = useQuery({
    queryKey: [...CUSTOMERS_QUERY_KEY, listParams.page, listParams.pageSize, listParams.search ?? ''],
    queryFn: () => fetchCustomers(listParams),
  });

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

  const refetch = async () => {
    await queryRefetch();
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

  const customers = pagedData?.items ?? [];
  const totalCount = pagedData?.totalCount ?? 0;
  const page = pagedData?.page ?? 1;
  const pageSize = pagedData?.pageSize ?? defaultListParams.pageSize!;
  const totalPages = pagedData?.totalPages ?? 0;
  const hasNextPage = pagedData?.hasNextPage ?? false;
  const hasPreviousPage = pagedData?.hasPreviousPage ?? false;

  return {
    customers,
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
    createCustomer,
    updateCustomer,
    deleteCustomer,
  };
}
