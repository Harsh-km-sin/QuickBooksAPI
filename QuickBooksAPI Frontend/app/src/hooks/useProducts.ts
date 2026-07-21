import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { productApi } from '@/api/productApi';
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

interface UseProductsListReturn {
  products: Products[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  isLoading: boolean;
  error: string | null;
}

/** Paged product list query only — pair with `useProductMutations` for create/update/delete/sync. */
export function useProductsList(listParams?: ListQueryParams): UseProductsListReturn {
  const params = { ...defaultListParams, ...listParams };

  const {
    data: pagedData,
    isLoading,
    error: queryError,
  } = useQuery({
    queryKey: [...PRODUCTS_QUERY_KEY, params.page, params.pageSize, params.search ?? '', params.activeFilter ?? 'active', params.sortBy ?? '', params.sortDir ?? ''],
    queryFn: () => fetchProducts(params),
  });

  return {
    products: pagedData?.items ?? [],
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

async function syncProducts() {
  const response = await productApi.sync();
  if (!response.success) {
    throw new Error(response.message || 'Failed to sync products');
  }
  return response.data;
}

interface UseProductMutationsReturn {
  isSyncing: boolean;
  sync: () => Promise<void>;
  createProduct: (data: CreateProductRequest) => Promise<boolean>;
  updateProduct: (data: UpdateProductRequest) => Promise<boolean>;
  deleteProduct: (data: DeleteProductRequest) => Promise<boolean>;
}

/** Sync/create/update/delete — no list query, safe to call from a page that doesn't own paging state. */
export function useProductMutations(): UseProductMutationsReturn {
  const queryClient = useQueryClient();

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

  return {
    isSyncing: syncMutation.isPending,
    sync,
    createProduct,
    updateProduct,
    deleteProduct,
  };
}
