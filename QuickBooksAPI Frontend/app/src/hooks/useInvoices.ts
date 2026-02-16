import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { invoiceApi } from '@/api/client';
import type {
  QBOInvoiceHeader,
  CreateInvoiceRequest,
  UpdateInvoiceRequest,
  DeleteInvoiceRequest,
  VoidInvoiceRequest,
  ListQueryParams,
} from '@/types';
import { toast } from 'sonner';

const INVOICES_QUERY_KEY = ['invoices'] as const;

const defaultListParams: ListQueryParams = { page: 1, pageSize: 20 };

async function fetchInvoices(params: ListQueryParams) {
  const response = await invoiceApi.list(params);
  if (!response.success || !response.data) {
    throw new Error(response.message || 'Failed to fetch invoices');
  }
  return response.data;
}

async function syncInvoices() {
  const response = await invoiceApi.sync();
  if (!response.success) {
    throw new Error(response.message || 'Failed to sync invoices');
  }
  return response.data;
}

interface UseInvoicesOptions {
  listParams?: ListQueryParams;
}

interface UseInvoicesReturn {
  invoices: QBOInvoiceHeader[];
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
  createInvoice: (data: CreateInvoiceRequest) => Promise<boolean>;
  updateInvoice: (data: UpdateInvoiceRequest) => Promise<boolean>;
  deleteInvoice: (data: DeleteInvoiceRequest) => Promise<boolean>;
  voidInvoice: (data: VoidInvoiceRequest) => Promise<boolean>;
}

export function useInvoices(options?: UseInvoicesOptions): UseInvoicesReturn {
  const queryClient = useQueryClient();
  const listParams = { ...defaultListParams, ...options?.listParams };

  const {
    data: pagedData,
    isLoading,
    error: queryError,
    refetch: queryRefetch,
  } = useQuery({
    queryKey: [...INVOICES_QUERY_KEY, listParams.page, listParams.pageSize, listParams.search ?? ''],
    queryFn: () => fetchInvoices(listParams),
  });

  const syncMutation = useMutation({
    mutationFn: syncInvoices,
    onSuccess: (count) => {
      toast.success('Sync completed', {
        description: `${count} invoices synced from QuickBooks`,
      });
      queryClient.invalidateQueries({ queryKey: INVOICES_QUERY_KEY });
    },
    onError: (err) => {
      toast.error('Sync failed', {
        description: err instanceof Error ? err.message : 'An unexpected error occurred',
      });
    },
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateInvoiceRequest) => invoiceApi.create(data),
    onSuccess: (response) => {
      if (response.success) {
        toast.success('Invoice created successfully');
        queryClient.invalidateQueries({ queryKey: INVOICES_QUERY_KEY });
      } else {
        toast.error('Failed to create invoice', { description: response.message || 'Please try again' });
      }
    },
    onError: (err) => {
      toast.error('Failed to create invoice', {
        description: err instanceof Error ? err.message : 'An unexpected error occurred',
      });
    },
  });

  const updateMutation = useMutation({
    mutationFn: (data: UpdateInvoiceRequest) => invoiceApi.update(data),
    onSuccess: (response) => {
      if (response.success) {
        toast.success('Invoice updated successfully');
        queryClient.invalidateQueries({ queryKey: INVOICES_QUERY_KEY });
      } else {
        toast.error('Failed to update invoice', { description: response.message || 'Please try again' });
      }
    },
    onError: (err) => {
      toast.error('Failed to update invoice', {
        description: err instanceof Error ? err.message : 'An unexpected error occurred',
      });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (data: DeleteInvoiceRequest) => invoiceApi.delete(data),
    onSuccess: (response) => {
      if (response.success) {
        toast.success('Invoice deleted successfully');
        queryClient.invalidateQueries({ queryKey: INVOICES_QUERY_KEY });
      } else {
        toast.error('Failed to delete invoice', { description: response.message || 'Please try again' });
      }
    },
    onError: (err) => {
      toast.error('Failed to delete invoice', {
        description: err instanceof Error ? err.message : 'An unexpected error occurred',
      });
    },
  });

  const voidMutation = useMutation({
    mutationFn: (data: VoidInvoiceRequest) => invoiceApi.void(data),
    onSuccess: (response) => {
      if (response.success) {
        toast.success('Invoice voided successfully');
        queryClient.invalidateQueries({ queryKey: INVOICES_QUERY_KEY });
      } else {
        toast.error('Failed to void invoice', { description: response.message || 'Please try again' });
      }
    },
    onError: (err) => {
      toast.error('Failed to void invoice', {
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

  const createInvoice = async (data: CreateInvoiceRequest): Promise<boolean> => {
    try {
      const response = await createMutation.mutateAsync(data);
      return response.success;
    } catch {
      return false;
    }
  };

  const updateInvoice = async (data: UpdateInvoiceRequest): Promise<boolean> => {
    try {
      const response = await updateMutation.mutateAsync(data);
      return response.success;
    } catch {
      return false;
    }
  };

  const deleteInvoice = async (data: DeleteInvoiceRequest): Promise<boolean> => {
    try {
      const response = await deleteMutation.mutateAsync(data);
      return response.success;
    } catch {
      return false;
    }
  };

  const voidInvoice = async (data: VoidInvoiceRequest): Promise<boolean> => {
    try {
      const response = await voidMutation.mutateAsync(data);
      return response.success;
    } catch {
      return false;
    }
  };

  const invoices = pagedData?.items ?? [];
  const totalCount = pagedData?.totalCount ?? 0;
  const page = pagedData?.page ?? 1;
  const pageSize = pagedData?.pageSize ?? defaultListParams.pageSize!;
  const totalPages = pagedData?.totalPages ?? 0;
  const hasNextPage = pagedData?.hasNextPage ?? false;
  const hasPreviousPage = pagedData?.hasPreviousPage ?? false;

  return {
    invoices,
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
    createInvoice,
    updateInvoice,
    deleteInvoice,
    voidInvoice,
  };
}
