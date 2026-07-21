import { useNavigate } from 'react-router-dom';
import { useInvoiceMutations } from '@/hooks';
import { useAppDispatch, useAppSelector } from '@/store/hooks';
import {
  openDeleteDialog,
  closeDeleteDialog,
  openVoidDialog,
  closeVoidDialog,
  setSubmitting,
} from '@/store/slices/invoiceUiSlice';
import type { QBOInvoiceHeader } from '@/types';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { RefreshCw, Loader2, ChevronLeft } from 'lucide-react';
import { InvoiceTable } from './InvoiceTable';

export function Invoices() {
  const navigate = useNavigate();
  const { isSyncing, sync, deleteInvoice, voidInvoice } = useInvoiceMutations();

  const dispatch = useAppDispatch();
  const {
    isDeleteDialogOpen,
    isVoidDialogOpen,
    selectedInvoice,
    isSubmitting,
  } = useAppSelector((state) => state.invoiceUi);

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
    <div className="flex h-[calc(100vh-3rem)] lg:h-[calc(100vh-4rem)] flex-col space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 shrink-0">
        <div className="flex items-center gap-2">
          <Button variant="ghost" size="icon" onClick={() => navigate('/settings/master-data')}>
            <ChevronLeft className="h-5 w-5" />
          </Button>
          <div><h1 className="text-3xl font-bold tracking-tight">Invoices</h1><p className="text-muted-foreground">Manage your customer invoices</p></div>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={sync} disabled={isSyncing}>{isSyncing ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <RefreshCw className="h-4 w-4 mr-2" />}Sync</Button>
        </div>
      </div>

      <InvoiceTable
        onOpenVoid={(invoice: QBOInvoiceHeader) => dispatch(openVoidDialog(invoice))}
        onOpenDelete={(invoice: QBOInvoiceHeader) => dispatch(openDeleteDialog(invoice))}
      />

      <Dialog open={isVoidDialogOpen} onOpenChange={(open) => !open && dispatch(closeVoidDialog())}>
        <DialogContent><DialogHeader><DialogTitle>Void Invoice</DialogTitle><DialogDescription>Are you sure you want to void this invoice for {selectedInvoice?.customerRefName}?</DialogDescription></DialogHeader>
        <DialogFooter><Button variant="outline" onClick={() => dispatch(closeVoidDialog())}>Cancel</Button><Button variant="secondary" onClick={handleVoid} disabled={isSubmitting}>{isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}Void Invoice</Button></DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={isDeleteDialogOpen} onOpenChange={(open) => !open && dispatch(closeDeleteDialog())}>
        <DialogContent><DialogHeader><DialogTitle>Delete Invoice</DialogTitle><DialogDescription>Are you sure you want to delete this invoice for {selectedInvoice?.customerRefName}?</DialogDescription></DialogHeader>
        <DialogFooter><Button variant="outline" onClick={() => dispatch(closeDeleteDialog())}>Cancel</Button><Button variant="destructive" onClick={handleDelete} disabled={isSubmitting}>{isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}Delete</Button></DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
