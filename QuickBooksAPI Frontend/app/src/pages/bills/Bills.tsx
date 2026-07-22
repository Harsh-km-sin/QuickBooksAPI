import { useState } from 'react';
import { useBillMutations } from '@/hooks';
import { useAppDispatch, useAppSelector } from '@/store/hooks';
import { openEditDialog, closeEditDialog, openDeleteDialog, closeDeleteDialog, setSubmitting } from '@/store/slices/billUiSlice';
import type { QBOBillHeader } from '@/types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { RefreshCw, Loader2 } from 'lucide-react';
import { BillTable } from './BillTable';

export function Bills() {
  const { isSyncing, sync, getBillById, updateBill, deleteBill } = useBillMutations();
  const [isLoadingBill, setIsLoadingBill] = useState(false);

  const dispatch = useAppDispatch();
  const { isEditDialogOpen, isDeleteDialogOpen, selectedBill, isSubmitting } = useAppSelector((state) => state.billUi);

  const handleOpenEditDialog = async (bill: QBOBillHeader) => {
    setIsLoadingBill(true);
    const fullBill = await getBillById(bill.qboBillId);
    setIsLoadingBill(false);
    if (fullBill) dispatch(openEditDialog(fullBill));
  };

  const handleUpdate = async (e: React.FormEvent<HTMLFormElement>) => {
    if (!selectedBill) return;
    e.preventDefault();
    const form = e.currentTarget;
    const txnDate = (form.elements.namedItem('txnDate') as HTMLInputElement)?.value;
    const dueDate = (form.elements.namedItem('dueDate') as HTMLInputElement)?.value;
    dispatch(setSubmitting(true));
    const success = await updateBill({
      id: selectedBill.qboBillId,
      syncToken: selectedBill.syncToken,
      ...(selectedBill.vendorRefValue && selectedBill.vendorRefName && { vendorRef: { value: selectedBill.vendorRefValue, name: selectedBill.vendorRefName } }),
      ...(txnDate && { txnDate }),
      ...(dueDate && { dueDate }),
    });
    dispatch(setSubmitting(false));
    if (success) dispatch(closeEditDialog());
  };

  const handleDelete = async () => {
    if (!selectedBill) return;
    dispatch(setSubmitting(true));
    const success = await deleteBill({ id: selectedBill.qboBillId, syncToken: selectedBill.syncToken });
    dispatch(setSubmitting(false));
    if (success) dispatch(closeDeleteDialog());
  };

  const formatCurrency = (value: number) => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);

  return (
    <div className="flex h-[calc(100vh-7rem)] lg:h-[calc(100vh-8rem)] flex-col space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 shrink-0">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Bills</h1>
          <p className="text-muted-foreground">Manage your vendor bills</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={sync} disabled={isSyncing}>{isSyncing ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <RefreshCw className="h-4 w-4 mr-2" />}Sync</Button>
        </div>
      </div>

      <BillTable
        onOpenEdit={handleOpenEditDialog}
        onOpenDelete={(bill: QBOBillHeader) => dispatch(openDeleteDialog(bill))}
        isLoadingBill={isLoadingBill}
      />

      <Dialog open={isEditDialogOpen} onOpenChange={(open) => !open && dispatch(closeEditDialog())}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>Edit Bill</DialogTitle>
            <DialogDescription>Update bill details</DialogDescription>
          </DialogHeader>
          {selectedBill && (
            <form onSubmit={handleUpdate} className="space-y-4">
              <div className="space-y-2">
                <Label>Vendor</Label>
                <p className="text-sm font-medium">{selectedBill.vendorRefName || '—'}</p>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="txnDate">Transaction Date</Label>
                  <Input
                    id="txnDate"
                    name="txnDate"
                    type="date"
                    defaultValue={selectedBill.txnDate ? selectedBill.txnDate.slice(0, 10) : ''}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="dueDate">Due Date</Label>
                  <Input
                    id="dueDate"
                    name="dueDate"
                    type="date"
                    defaultValue={selectedBill.dueDate ? selectedBill.dueDate.slice(0, 10) : ''}
                  />
                </div>
              </div>
              <div className="space-y-2">
                <Label>Total / Balance</Label>
                <p className="text-sm">{formatCurrency(selectedBill.totalAmt)} / {formatCurrency(selectedBill.balance)}</p>
              </div>
              <DialogFooter>
                <Button type="button" variant="outline" onClick={() => dispatch(closeEditDialog())}>Cancel</Button>
                <Button type="submit" disabled={isSubmitting}>{isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}Save</Button>
              </DialogFooter>
            </form>
          )}
        </DialogContent>
      </Dialog>

      <Dialog open={isDeleteDialogOpen} onOpenChange={(open) => !open && dispatch(closeDeleteDialog())}>
        <DialogContent><DialogHeader><DialogTitle>Delete Bill</DialogTitle><DialogDescription>Are you sure you want to delete this bill from {selectedBill?.vendorRefName}?</DialogDescription></DialogHeader>
          <DialogFooter><Button variant="outline" onClick={() => dispatch(closeDeleteDialog())}>Cancel</Button><Button variant="destructive" onClick={handleDelete} disabled={isSubmitting}>{isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}Delete</Button></DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
