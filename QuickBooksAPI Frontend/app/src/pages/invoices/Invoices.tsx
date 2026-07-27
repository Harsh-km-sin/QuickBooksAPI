import { useInvoiceMutations } from '@/hooks';
import { useAppDispatch, useAppSelector } from '@/store/hooks';
import {
  openCreateDialog,
  closeCreateDialog,
  openEditDialog,
  closeEditDialog,
  openDeleteDialog,
  closeDeleteDialog,
  openVoidDialog,
  closeVoidDialog,
  setSubmitting,
} from '@/store/slices/invoiceUiSlice';
import { InvoiceForm } from '@/features/invoices';
import type { QBOInvoiceHeader, CreateInvoiceRequest, UpdateInvoiceRequest } from '@/types';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Plus, RefreshCw, Loader2 } from 'lucide-react';
import { InvoiceTable } from './InvoiceTable';

export function Invoices() {
  const { isSyncing, sync, createInvoice, updateInvoice, deleteInvoice, voidInvoice } = useInvoiceMutations();

  const dispatch = useAppDispatch();
  const {
    isCreateDialogOpen,
    isEditDialogOpen,
    isDeleteDialogOpen,
    isVoidDialogOpen,
    selectedInvoice,
    isSubmitting,
  } = useAppSelector((state) => state.invoiceUi);

  const handleCreate = async (data: CreateInvoiceRequest | UpdateInvoiceRequest) => {
    dispatch(setSubmitting(true));
    const success = await createInvoice(data as CreateInvoiceRequest);
    dispatch(setSubmitting(false));
    if (success) dispatch(closeCreateDialog());
  };

  const handleUpdate = async (data: CreateInvoiceRequest | UpdateInvoiceRequest) => {
    dispatch(setSubmitting(true));
    const success = await updateInvoice(data as UpdateInvoiceRequest);
    dispatch(setSubmitting(false));
    if (success) dispatch(closeEditDialog());
  };

  const handleDelete = async () => {
    if (!selectedInvoice) return;
    dispatch(setSubmitting(true));
    const success = await deleteInvoice({ id: selectedInvoice.qboInvoiceId, syncToken: selectedInvoice.syncToken });
    dispatch(setSubmitting(false));
    if (success) dispatch(closeDeleteDialog());
  };

  const handleVoid = async () => {
    if (!selectedInvoice) return;
    dispatch(setSubmitting(true));
    const success = await voidInvoice({ id: selectedInvoice.qboInvoiceId, syncToken: selectedInvoice.syncToken });
    dispatch(setSubmitting(false));
    if (success) dispatch(closeVoidDialog());
  };

  return (
    <div className="flex h-[calc(100vh-7rem)] lg:h-[calc(100vh-8rem)] flex-col space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 shrink-0">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Invoices</h1>
          <p className="text-muted-foreground">Manage your customer invoices</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={sync} disabled={isSyncing}>
            {isSyncing ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <RefreshCw className="h-4 w-4 mr-2" />}
            Sync
          </Button>
          <Button onClick={() => dispatch(openCreateDialog())}>
            <Plus className="h-4 w-4 mr-2" />
            Create Invoice
          </Button>
        </div>
      </div>

      <InvoiceTable
        onOpenEdit={(invoice: QBOInvoiceHeader) => dispatch(openEditDialog(invoice))}
        onOpenVoid={(invoice: QBOInvoiceHeader) => dispatch(openVoidDialog(invoice))}
        onOpenDelete={(invoice: QBOInvoiceHeader) => dispatch(openDeleteDialog(invoice))}
      />

      <Dialog open={isCreateDialogOpen} onOpenChange={(open) => !open && dispatch(closeCreateDialog())}>
        <DialogContent className="max-h-[90vh] overflow-y-auto" style={{ maxWidth: '850px' }}>
          <DialogHeader>
            <DialogTitle>Create Invoice</DialogTitle>
            <DialogDescription>
              Create a new invoice for your customer in QuickBooks
            </DialogDescription>
          </DialogHeader>
          <InvoiceForm
            onSubmit={handleCreate}
            onCancel={() => dispatch(closeCreateDialog())}
            isSubmitting={isSubmitting}
          />
        </DialogContent>
      </Dialog>

      <Dialog open={isEditDialogOpen} onOpenChange={(open) => !open && dispatch(closeEditDialog())}>
        <DialogContent className="max-h-[90vh] overflow-y-auto" style={{ maxWidth: '850px' }}>
          <DialogHeader>
            <DialogTitle>Edit Invoice</DialogTitle>
            <DialogDescription>
              Update invoice details in QuickBooks
            </DialogDescription>
          </DialogHeader>
          {selectedInvoice && (
            <InvoiceForm
              invoice={selectedInvoice}
              onSubmit={handleUpdate}
              onCancel={() => dispatch(closeEditDialog())}
              isSubmitting={isSubmitting}
            />
          )}
        </DialogContent>
      </Dialog>

      <Dialog open={isVoidDialogOpen} onOpenChange={(open) => !open && dispatch(closeVoidDialog())}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Void Invoice</DialogTitle>
            <DialogDescription>Are you sure you want to void this invoice for {selectedInvoice?.customerRefName}?</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => dispatch(closeVoidDialog())}>Cancel</Button>
            <Button variant="secondary" onClick={handleVoid} disabled={isSubmitting}>
              {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Void Invoice
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={isDeleteDialogOpen} onOpenChange={(open) => !open && dispatch(closeDeleteDialog())}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Invoice</DialogTitle>
            <DialogDescription>Are you sure you want to delete this invoice for {selectedInvoice?.customerRefName}?</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => dispatch(closeDeleteDialog())}>Cancel</Button>
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
