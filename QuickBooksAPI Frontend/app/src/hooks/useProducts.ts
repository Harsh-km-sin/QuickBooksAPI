import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { productApi } from '@/api/client';
import type { Products, CreateProductRequest, UpdateProductRequest, DeleteProductRequest, ListQueryParams } from '@/types';
import { toast } from 'sonner';

const PRODUCTS_QUERY_KEY = ['products'] as const;

const defaultListParams: ListQueryParams = { page: 1, pageSize: 20 };

async function fetchProducts(params: ListQueryParams) {
  const response = await productApi.list(params);
  if (!response.success || !response.data) {
    throw new Error(response.message || 'Failed to fetch products');
  }
  return response.data;
}

async function syncProducts() {
  const response = await productApi.sync();
  if (!response.success) {
    throw new Error(response.message || 'Failed to sync products');
  }
  return response.data;
}

interface UseProductsOptions {
  listParams?: ListQueryParams;
}

interface UseProductsReturn {
  products: Products[];
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
  createProduct: (data: CreateProductRequest) => Promise<boolean>;
  updateProduct: (data: UpdateProductRequest) => Promise<boolean>;
  deleteProduct: (data: DeleteProductRequest) => Promise<boolean>;
}

export function useProducts(options?: UseProductsOptions): UseProductsReturn {
  const queryClient = useQueryClient();
  const listParams = { ...defaultListParams, ...options?.listParams };

  const {
    data: pagedData,
    isLoading,
    error: queryError,
    refetch: queryRefetch,
  } = useQuery({
    queryKey: [...PRODUCTS_QUERY_KEY, listParams.page, listParams.pageSize, listParams.search ?? '', listParams.activeFilter ?? 'active'],
    queryFn: () => fetchProducts(listParams),
  });

  const syncMutation = useMutation({
    mutationFn: syncProducts,
    onSuccess: (count) => {
      toast.success('Sync completed', {
        description: `${count} products synced from QuickBooks`,
      });
      queryClient.invalidateQueries({ queryKey: PRODUCTS_QUERY_KEY });
    },
    onError: (err) => {
      toast.error('Sync failed', {
        description: err instanceof Error ? err.message : 'An unexpected error occurred',
      });
    },
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateProductRequest) => productApi.create(data),
    onSuccess: (response) => {
      if (response.success) {
        toast.success('Product created successfully');
        queryClient.invalidateQueries({ queryKey: PRODUCTS_QUERY_KEY });
      } else {
        toast.error('Failed to create product', {
          description: response.message || 'Please try again',
        });
      }
    },
    onError: (err) => {
      toast.error('Failed to create product', {
        description: err instanceof Error ? err.message : 'An unexpected error occurred',
      });
    },
  });

  const updateMutation = useMutation({
    mutationFn: (data: UpdateProductRequest) => productApi.update(data),
    onSuccess: (response) => {
      if (response.success) {
        toast.success('Product updated successfully');
        queryClient.invalidateQueries({ queryKey: PRODUCTS_QUERY_KEY });
      } else {
        toast.error('Failed to update product', {
          description: response.message || 'Please try again',
        });
      }
    },
    onError: (err) => {
      toast.error('Failed to update product', {
        description: err instanceof Error ? err.message : 'An unexpected error occurred',
      });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (data: DeleteProductRequest) => productApi.delete(data),
    onSuccess: (response) => {
      if (response.success) {
        toast.success('Product deleted successfully');
        queryClient.invalidateQueries({ queryKey: PRODUCTS_QUERY_KEY });
      } else {
        toast.error('Failed to delete product', {
          description: response.message || 'Please try again',
        });
      }
    },
    onError: (err) => {
      toast.error('Failed to delete product', {
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

  const createProduct = async (data: CreateProductRequest): Promise<boolean> => {
    try {
      const response = await createMutation.mutateAsync(data);
      return response.success;
    } catch {
      return false;
    }
  };

  const updateProduct = async (data: UpdateProductRequest): Promise<boolean> => {
    try {
      const response = await updateMutation.mutateAsync(data);
      return response.success;
    } catch {
      return false;
    }
  };

  const deleteProduct = async (data: DeleteProductRequest): Promise<boolean> => {
    try {
      const response = await deleteMutation.mutateAsync(data);
      return response.success;
    } catch {
      return false;
    }
  };

  const products = pagedData?.items ?? [];
  const totalCount = pagedData?.totalCount ?? 0;
  const page = pagedData?.page ?? 1;
  const pageSize = pagedData?.pageSize ?? defaultListParams.pageSize!;
  const totalPages = pagedData?.totalPages ?? 0;
  const hasNextPage = pagedData?.hasNextPage ?? false;
  const hasPreviousPage = pagedData?.hasPreviousPage ?? false;

  return {
    products,
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
    createProduct,
    updateProduct,
    deleteProduct,
  };
}
