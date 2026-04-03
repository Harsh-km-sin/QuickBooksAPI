import { useState, useMemo, useEffect } from 'react';
import { useCustomers, useDebouncedValue } from '@/hooks';
import { useAppDispatch, useAppSelector } from '@/store/hooks';
import {
  openCreateDialog,
  closeCreateDialog,
  openEditDialog,
  closeEditDialog,
  openDeleteDialog,
  closeDeleteDialog,
  setSubmitting,
} from '@/store/slices/customerUiSlice';
import { CustomerForm } from '@/features/customers';
import type { Customer, CreateCustomerRequest, UpdateCustomerRequest } from '@/types';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Skeleton } from '@/components/ui/skeleton';
import { Plus, RefreshCw, Loader2 } from 'lucide-react';
import { CustomersListCard } from './customers/CustomersListCard';

const SEARCH_DEBOUNCE_MS = 300;
const DEFAULT_PAGE_SIZE = 20;

export function Customers() {
  const [searchTerm, setSearchTerm] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);
  const [activeFilter, setActiveFilter] = useState<'active' | 'inactive' | 'all'>('active');
  const debouncedSearch = useDebouncedValue(searchTerm.trim(), SEARCH_DEBOUNCE_MS);

  const listParams = useMemo(
    () => ({ page, pageSize, search: debouncedSearch || undefined, activeFilter }),
    [page, pageSize, debouncedSearch, activeFilter]
  );
  const {
    customers,
    totalCount,
    page: currentPage,
    totalPages,
    hasNextPage,
    hasPreviousPage,
    isLoading,
    isSyncing,
    getCustomerById,
    createCustomer,
    updateCustomer,
    deleteCustomer,
    sync,
  } = useCustomers({ listParams });
  const [isLoadingCustomer, setIsLoadingCustomer] = useState(false);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, pageSize, activeFilter]);

  const goToPage = (nextPage: number) => setPage(() => Math.max(1, Math.min(nextPage, totalPages || 1)));

  const dispatch = useAppDispatch();
  const {
    isCreateDialogOpen,
    isEditDialogOpen,
    isDeleteDialogOpen,
    selectedCustomer,
    isSubmitting,
  } = useAppSelector((state) => state.customerUi);

  const handleCreate = async (data: CreateCustomerRequest | UpdateCustomerRequest) => {
    dispatch(setSubmitting(true));
    const success = await createCustomer(data as CreateCustomerRequest);
    dispatch(setSubmitting(false));
    if (success) dispatch(closeCreateDialog());
  };

  const handleUpdate = async (data: CreateCustomerRequest | UpdateCustomerRequest) => {
    dispatch(setSubmitting(true));
    const success = await updateCustomer(data as UpdateCustomerRequest);
    dispatch(setSubmitting(false));
    if (success) dispatch(closeEditDialog());
  };

  const handleDelete = async () => {
    if (!selectedCustomer) return;
    dispatch(setSubmitting(true));
    const success = await deleteCustomer({
      id: selectedCustomer.qboId,
      syncToken: selectedCustomer.syncToken,
      sparse: true,
      active: false,
    });
    dispatch(setSubmitting(false));
    if (success) dispatch(closeDeleteDialog());
  };

  const handleOpenEditDialog = async (customer: Customer) => {
    setIsLoadingCustomer(true);
    const fullCustomer = await getCustomerById(customer.qboId);
    setIsLoadingCustomer(false);
    if (fullCustomer) dispatch(openEditDialog(fullCustomer));
  };

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(value);
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Customers</h1>
            <p className="text-muted-foreground">Manage your customer accounts</p>
          </div>
          <Skeleton className="h-10 w-32" />
        </div>
        <Card>
          <CardContent className="p-6">
            <Skeleton className="h-[400px] w-full" />
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Customers</h1>
          <p className="text-muted-foreground">Manage your customer accounts</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={sync} disabled={isSyncing} className="hover:bg-muted hover:text-foreground">
            {isSyncing ? (
              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
            ) : (
              <RefreshCw className="h-4 w-4 mr-2" />
            )}
            Sync
          </Button>
          <Button onClick={() => dispatch(openCreateDialog())}>
            <Plus className="h-4 w-4 mr-2" />
            Add Customer
          </Button>
        </div>
      </div>

      <CustomersListCard
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        activeFilter={activeFilter}
        onActiveFilterChange={setActiveFilter}
        totalCount={totalCount}
        customers={customers}
        debouncedSearch={debouncedSearch}
        onOpenCreate={() => dispatch(openCreateDialog())}
        formatCurrency={formatCurrency}
        onOpenEdit={handleOpenEditDialog}
        onOpenDelete={(c) => dispatch(openDeleteDialog(c))}
        isLoadingCustomer={isLoadingCustomer}
        currentPage={currentPage}
        pageSize={pageSize}
        onPageSizeChange={setPageSize}
        totalPages={totalPages}
        hasNextPage={hasNextPage}
        hasPreviousPage={hasPreviousPage}
        onGoToPage={goToPage}
      />

      <Dialog open={isCreateDialogOpen} onOpenChange={(open) => !open && dispatch(closeCreateDialog())}>
        <DialogContent className="max-h-[90vh] overflow-y-auto" style={{ maxWidth: '800px' }}>
          <DialogHeader>
            <DialogTitle>Add Customer</DialogTitle>
            <DialogDescription>
              Create a new customer in your QuickBooks account
            </DialogDescription>
          </DialogHeader>
          <CustomerForm
            onSubmit={handleCreate}
            onCancel={() => dispatch(closeCreateDialog())}
            isSubmitting={isSubmitting}
          />
        </DialogContent>
      </Dialog>

      <Dialog open={isEditDialogOpen} onOpenChange={(open) => !open && dispatch(closeEditDialog())}>
        <DialogContent className="max-h-[90vh] overflow-y-auto" style={{ maxWidth: '900px' }}>
          <DialogHeader>
            <DialogTitle>Edit Customer</DialogTitle>
            <DialogDescription>
              Update customer information
            </DialogDescription>
          </DialogHeader>
          {selectedCustomer && (
            <CustomerForm
              customer={selectedCustomer}
              onSubmit={handleUpdate}
              onCancel={() => dispatch(closeEditDialog())}
              isSubmitting={isSubmitting}
            />
          )}
        </DialogContent>
      </Dialog>

      <Dialog open={isDeleteDialogOpen} onOpenChange={(open) => !open && dispatch(closeDeleteDialog())}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Customer</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete {selectedCustomer?.displayName || `${selectedCustomer?.givenName} ${selectedCustomer?.familyName}`}?
              This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => dispatch(closeDeleteDialog())}>
              Cancel
            </Button>
            <Button variant="destructive" onClick={handleDelete} disabled={isSubmitting}>
              {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
