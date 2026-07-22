import { useNavigate } from 'react-router-dom';
import { useVendorMutations } from '@/hooks';
import { useAppDispatch, useAppSelector } from '@/store/hooks';
import {
  openCreateDialog,
  closeCreateDialog,
  openEditDialog,
  closeEditDialog,
  openDeleteDialog,
  closeDeleteDialog,
  setSubmitting,
} from '@/store/slices/vendorUiSlice';
import { VendorForm } from '@/features/vendors';
import type { Vendor, CreateVendorRequest, UpdateVendorRequest } from '@/types';
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
import { VendorTable } from './VendorTable';

export function Vendors() {
  const navigate = useNavigate();
  const { isSyncing, sync, createVendor, updateVendor, softDeleteVendor } = useVendorMutations();

  const dispatch = useAppDispatch();
  const {
    isCreateDialogOpen,
    isEditDialogOpen,
    isDeleteDialogOpen,
    selectedVendor,
    isSubmitting,
  } = useAppSelector((state) => state.vendorUi);

  const handleCreate = async (data: CreateVendorRequest | UpdateVendorRequest) => {
    dispatch(setSubmitting(true));
    const success = await createVendor(data as CreateVendorRequest);
    dispatch(setSubmitting(false));
    if (success) dispatch(closeCreateDialog());
  };

  const handleUpdate = async (data: CreateVendorRequest | UpdateVendorRequest) => {
    dispatch(setSubmitting(true));
    const success = await updateVendor(data as UpdateVendorRequest);
    dispatch(setSubmitting(false));
    if (success) dispatch(closeEditDialog());
  };

  const handleDelete = async () => {
    if (!selectedVendor) return;
    dispatch(setSubmitting(true));
    const success = await softDeleteVendor({ id: selectedVendor.qboId, syncToken: selectedVendor.syncToken });
    dispatch(setSubmitting(false));
    if (success) dispatch(closeDeleteDialog());
  };

  return (
    <div className="flex h-[calc(100vh-7rem)] lg:h-[calc(100vh-8rem)] flex-col space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 shrink-0">
        <div className="flex items-center gap-2">
          <Button variant="ghost" size="icon" onClick={() => navigate('/settings/master-data')}>
            <ChevronLeft className="h-5 w-5" />
          </Button>
          <div><h1 className="text-3xl font-bold tracking-tight">Vendors</h1><p className="text-muted-foreground">Manage your vendors and suppliers</p></div>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={sync} disabled={isSyncing}>{isSyncing ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <RefreshCw className="h-4 w-4 mr-2" />}Sync</Button>
          <Button onClick={() => dispatch(openCreateDialog())}><Plus className="h-4 w-4 mr-2" />Add Vendor</Button>
        </div>
      </div>

      <VendorTable
        onOpenCreate={() => dispatch(openCreateDialog())}
        onOpenEdit={(vendor: Vendor) => dispatch(openEditDialog(vendor))}
        onOpenDelete={(vendor: Vendor) => dispatch(openDeleteDialog(vendor))}
      />

      <Dialog open={isCreateDialogOpen} onOpenChange={(open) => !open && dispatch(closeCreateDialog())}>
        <DialogContent className="max-h-[90vh] overflow-y-auto" style={{ maxWidth: '800px' }}><DialogHeader><DialogTitle>Add Vendor</DialogTitle><DialogDescription>Create a new vendor in your QuickBooks account</DialogDescription></DialogHeader><VendorForm onSubmit={handleCreate} onCancel={() => dispatch(closeCreateDialog())} isSubmitting={isSubmitting} /></DialogContent>
      </Dialog>

      <Dialog open={isEditDialogOpen} onOpenChange={(open) => !open && dispatch(closeEditDialog())}>
        <DialogContent className="max-h-[90vh] overflow-y-auto" style={{ maxWidth: '800px' }}><DialogHeader><DialogTitle>Edit Vendor</DialogTitle><DialogDescription>Update vendor information</DialogDescription></DialogHeader>{selectedVendor && <VendorForm vendor={selectedVendor} onSubmit={handleUpdate} onCancel={() => dispatch(closeEditDialog())} isSubmitting={isSubmitting} />}</DialogContent>
      </Dialog>

      <Dialog open={isDeleteDialogOpen} onOpenChange={(open) => !open && dispatch(closeDeleteDialog())}>
        <DialogContent><DialogHeader><DialogTitle>Delete Vendor</DialogTitle><DialogDescription>Are you sure you want to delete {selectedVendor?.displayName || `${selectedVendor?.givenName} ${selectedVendor?.familyName}`}?</DialogDescription></DialogHeader>
          <DialogFooter><Button variant="outline" onClick={() => dispatch(closeDeleteDialog())}>Cancel</Button><Button variant="destructive" onClick={handleDelete} disabled={isSubmitting}>{isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}Delete</Button></DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
