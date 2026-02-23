import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { vendorApi } from '@/api/client';
import type { Vendor, CreateVendorRequest, UpdateVendorRequest, SoftDeleteVendorRequest, ListQueryParams } from '@/types';
import { toast } from 'sonner';

const VENDORS_QUERY_KEY = ['vendors'] as const;

const defaultListParams: ListQueryParams = { page: 1, pageSize: 20 };

async function fetchVendors(params: ListQueryParams) {
  const response = await vendorApi.list(params);
  if (!response.success || !response.data) {
    throw new Error(response.message || 'Failed to fetch vendors');
  }
  return response.data;
}

async function syncVendors() {
  const response = await vendorApi.sync();
  if (!response.success) {
    throw new Error(response.message || 'Failed to sync vendors');
  }
  return response.data;
}

interface UseVendorsOptions {
  listParams?: ListQueryParams;
}

interface UseVendorsReturn {
  vendors: Vendor[];
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
  createVendor: (data: CreateVendorRequest) => Promise<boolean>;
  updateVendor: (data: UpdateVendorRequest) => Promise<boolean>;
  softDeleteVendor: (data: SoftDeleteVendorRequest) => Promise<boolean>;
}

export function useVendors(options?: UseVendorsOptions): UseVendorsReturn {
  const queryClient = useQueryClient();
  const listParams = { ...defaultListParams, ...options?.listParams };

  const {
    data: pagedData,
    isLoading,
    error: queryError,
    refetch: queryRefetch,
  } = useQuery({
    queryKey: [...VENDORS_QUERY_KEY, listParams.page, listParams.pageSize, listParams.search ?? '', listParams.activeFilter ?? 'active'],
    queryFn: () => fetchVendors(listParams),
  });

  const syncMutation = useMutation({
    mutationFn: syncVendors,
    onSuccess: (count) => {
      toast.success('Sync completed', {
        description: `${count} vendors synced from QuickBooks`,
      });
      queryClient.invalidateQueries({ queryKey: VENDORS_QUERY_KEY });
    },
    onError: (err) => {
      toast.error('Sync failed', {
        description: err instanceof Error ? err.message : 'An unexpected error occurred',
      });
    },
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateVendorRequest) => vendorApi.create(data),
    onSuccess: (response) => {
      if (response.success) {
        toast.success('Vendor created successfully');
        queryClient.invalidateQueries({ queryKey: VENDORS_QUERY_KEY });
      } else {
        toast.error('Failed to create vendor', {
          description: response.message || 'Please try again',
        });
      }
    },
    onError: (err) => {
      toast.error('Failed to create vendor', {
        description: err instanceof Error ? err.message : 'An unexpected error occurred',
      });
    },
  });

  const updateMutation = useMutation({
    mutationFn: (data: UpdateVendorRequest) => vendorApi.update(data),
    onSuccess: (response) => {
      if (response.success) {
        toast.success('Vendor updated successfully');
        queryClient.invalidateQueries({ queryKey: VENDORS_QUERY_KEY });
      } else {
        toast.error('Failed to update vendor', {
          description: response.message || 'Please try again',
        });
      }
    },
    onError: (err) => {
      toast.error('Failed to update vendor', {
        description: err instanceof Error ? err.message : 'An unexpected error occurred',
      });
    },
  });

  const softDeleteMutation = useMutation({
    mutationFn: (data: SoftDeleteVendorRequest) => vendorApi.softDelete(data),
    onSuccess: (response) => {
      if (response.success) {
        toast.success('Vendor deleted successfully');
        queryClient.invalidateQueries({ queryKey: VENDORS_QUERY_KEY });
      } else {
        toast.error('Failed to delete vendor', {
          description: response.message || 'Please try again',
        });
      }
    },
    onError: (err) => {
      toast.error('Failed to delete vendor', {
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

  const createVendor = async (data: CreateVendorRequest): Promise<boolean> => {
    try {
      const response = await createMutation.mutateAsync(data);
      return response.success;
    } catch {
      return false;
    }
  };

  const updateVendor = async (data: UpdateVendorRequest): Promise<boolean> => {
    try {
      const response = await updateMutation.mutateAsync(data);
      return response.success;
    } catch {
      return false;
    }
  };

  const softDeleteVendor = async (data: SoftDeleteVendorRequest): Promise<boolean> => {
    try {
      const response = await softDeleteMutation.mutateAsync(data);
      return response.success;
    } catch {
      return false;
    }
  };

  const vendors = pagedData?.items ?? [];
  const totalCount = pagedData?.totalCount ?? 0;
  const page = pagedData?.page ?? 1;
  const pageSize = pagedData?.pageSize ?? defaultListParams.pageSize!;
  const totalPages = pagedData?.totalPages ?? 0;
  const hasNextPage = pagedData?.hasNextPage ?? false;
  const hasPreviousPage = pagedData?.hasPreviousPage ?? false;

  return {
    vendors,
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
    createVendor,
    updateVendor,
    softDeleteVendor,
  };
}
