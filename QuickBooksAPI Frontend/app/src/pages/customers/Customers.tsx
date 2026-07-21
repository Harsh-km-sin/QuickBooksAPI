import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useCustomerMutations } from '@/hooks';
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
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Plus, RefreshCw, Loader2, ChevronLeft } from 'lucide-react';
import { CustomerTable } from './CustomerTable';

export function Customers() {
  const navigate = useNavigate();
  const { isSyncing, sync, getCustomerById, createCustomer, updateCustomer, deleteCustomer } = useCustomerMutations();
  const [isLoadingCustomer, setIsLoadingCustomer] = useState(false);

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

  return (
    <div className="flex h-[calc(100vh-3rem)] lg:h-[calc(100vh-4rem)] flex-col space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 shrink-0">
        <div className="flex items-center gap-2">
          <Button variant="ghost" size="icon" onClick={() => navigate('/settings/master-data')}>
            <ChevronLeft className="h-5 w-5" />
          </Button>
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Customers</h1>
            <p className="text-muted-foreground">Manage your customer accounts</p>
          </div>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={sync} disabled={isSyncing}>
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

      <CustomerTable
        onOpenCreate={() => dispatch(openCreateDialog())}
        onOpenEdit={handleOpenEditDialog}
        onOpenDelete={(c) => dispatch(openDeleteDialog(c))}
        isLoadingCustomer={isLoadingCustomer}
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
