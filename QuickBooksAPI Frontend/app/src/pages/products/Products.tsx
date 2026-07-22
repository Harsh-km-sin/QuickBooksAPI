import { useNavigate } from 'react-router-dom';
import { useProductMutations } from '@/hooks';
import { useAppDispatch, useAppSelector } from '@/store/hooks';
import {
  openCreateDialog,
  closeCreateDialog,
  openEditDialog,
  closeEditDialog,
  openDeleteDialog,
  closeDeleteDialog,
  setSubmitting,
} from '@/store/slices/productUiSlice';
import { ProductForm } from '@/features/products';
import type { Products, CreateProductRequest, UpdateProductRequest } from '@/types';
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
import { ProductTable } from './ProductTable';

export function ProductsPage() {
  const navigate = useNavigate();
  const { isSyncing, sync, createProduct, updateProduct, deleteProduct } = useProductMutations();

  const dispatch = useAppDispatch();
  const {
    isCreateDialogOpen,
    isEditDialogOpen,
    isDeleteDialogOpen,
    selectedProduct,
    isSubmitting,
  } = useAppSelector((state) => state.productUi);

  const handleCreate = async (data: CreateProductRequest | UpdateProductRequest) => {
    dispatch(setSubmitting(true));
    const success = await createProduct(data as CreateProductRequest);
    dispatch(setSubmitting(false));
    if (success) dispatch(closeCreateDialog());
  };

  const handleUpdate = async (data: CreateProductRequest | UpdateProductRequest) => {
    dispatch(setSubmitting(true));
    const success = await updateProduct(data as UpdateProductRequest);
    dispatch(setSubmitting(false));
    if (success) dispatch(closeEditDialog());
  };

  const handleDelete = async () => {
    if (!selectedProduct) return;
    dispatch(setSubmitting(true));
    const success = await deleteProduct({
      id: selectedProduct.qboId,
      syncToken: selectedProduct.syncToken,
      sparse: true,
      active: false,
    });
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
          <div><h1 className="text-3xl font-bold tracking-tight">Products</h1><p className="text-muted-foreground">Manage your products and services</p></div>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={sync} disabled={isSyncing}>
            {isSyncing ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <RefreshCw className="h-4 w-4 mr-2" />}
            Sync
          </Button>
          <Button onClick={() => dispatch(openCreateDialog())}>
            <Plus className="h-4 w-4 mr-2" />
            Add Product
          </Button>
        </div>
      </div>

      <ProductTable
        onOpenCreate={() => dispatch(openCreateDialog())}
        onOpenEdit={(product: Products) => dispatch(openEditDialog(product))}
        onOpenDelete={(product: Products) => dispatch(openDeleteDialog(product))}
      />

      <Dialog open={isCreateDialogOpen} onOpenChange={(open) => !open && dispatch(closeCreateDialog())}>
        <DialogContent className="max-h-[90vh] overflow-y-auto" style={{ maxWidth: '700px' }}><DialogHeader><DialogTitle>Add Product</DialogTitle><DialogDescription>Create a new product or service</DialogDescription></DialogHeader><ProductForm onSubmit={handleCreate} onCancel={() => dispatch(closeCreateDialog())} isSubmitting={isSubmitting} /></DialogContent>
      </Dialog>
      <Dialog open={isEditDialogOpen} onOpenChange={(open) => !open && dispatch(closeEditDialog())}>
        <DialogContent className="max-h-[90vh] overflow-y-auto" style={{ maxWidth: '700px' }}><DialogHeader><DialogTitle>Edit Product</DialogTitle><DialogDescription>Update product information</DialogDescription></DialogHeader>{selectedProduct && <ProductForm product={selectedProduct} onSubmit={handleUpdate} onCancel={() => dispatch(closeEditDialog())} isSubmitting={isSubmitting} />}</DialogContent>
      </Dialog>
      <Dialog open={isDeleteDialogOpen} onOpenChange={(open) => !open && dispatch(closeDeleteDialog())}>
        <DialogContent><DialogHeader><DialogTitle>Delete Product</DialogTitle><DialogDescription>Are you sure you want to delete {selectedProduct?.name}?</DialogDescription></DialogHeader><DialogFooter><Button variant="outline" onClick={() => dispatch(closeDeleteDialog())}>Cancel</Button><Button variant="destructive" onClick={handleDelete} disabled={isSubmitting}>{isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}Delete</Button></DialogFooter></DialogContent>
      </Dialog>
    </div>
  );
}
