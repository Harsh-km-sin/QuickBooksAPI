import { useState, useMemo, useEffect } from 'react';
import { useProducts, useDebouncedValue } from '@/hooks';
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
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Search,
  Plus,
  MoreHorizontal,
  RefreshCw,
  Loader2,
  Edit,
  Trash2,
  Package,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react';

const SEARCH_DEBOUNCE_MS = 300;
const PAGE_SIZE_OPTIONS = [10, 20, 50, 100] as const;
const DEFAULT_PAGE_SIZE = 20;

export function ProductsPage() {
  const [searchTerm, setSearchTerm] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);
  const debouncedSearch = useDebouncedValue(searchTerm.trim(), SEARCH_DEBOUNCE_MS);

  const listParams = useMemo(
    () => ({ page, pageSize, search: debouncedSearch || undefined }),
    [page, pageSize, debouncedSearch]
  );
  const {
    products,
    totalCount,
    page: currentPage,
    pageSize: currentPageSize,
    totalPages,
    hasNextPage,
    hasPreviousPage,
    isLoading,
    isSyncing,
    createProduct,
    updateProduct,
    deleteProduct,
    sync,
  } = useProducts({ listParams });

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, pageSize]);

  const goToPage = (nextPage: number) => setPage(() => Math.max(1, Math.min(nextPage, totalPages || 1)));

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
      active: false,
      type: selectedProduct.type,
      incomeAccountRef: { value: selectedProduct.incomeAccountRefValue || '', name: selectedProduct.incomeAccountRefName || '' },
    });
    dispatch(setSubmitting(false));
    if (success) dispatch(closeDeleteDialog());
  };

  const handleOpenEditDialog = (product: Products) => {
    dispatch(openEditDialog(product));
  };

  const handleOpenDeleteDialog = (product: Products) => {
    dispatch(openDeleteDialog(product));
  };

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Products</h1>
            <p className="text-muted-foreground">Manage your products and services</p>
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
          <h1 className="text-3xl font-bold tracking-tight">Products</h1>
          <p className="text-muted-foreground">Manage your products and services</p>
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

      <Card>
        <CardHeader>
          <div className="flex items-center gap-4">
            <div className="relative flex-1 max-w-sm">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input placeholder="Search products..." value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} className="pl-9" />
            </div>
            <Badge variant="secondary">{totalCount} product{totalCount !== 1 ? 's' : ''}</Badge>
          </div>
        </CardHeader>
        <CardContent>
          {products.length === 0 ? (
            <div className="text-center py-12">
              <Package className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
              <h3 className="text-lg font-semibold mb-2">No products found</h3>
              <p className="text-muted-foreground mb-4">{debouncedSearch ? 'Try adjusting your search' : 'Get started by adding your first product'}</p>
              {!debouncedSearch && <Button onClick={() => dispatch(openCreateDialog())}><Plus className="h-4 w-4 mr-2" />Add Product</Button>}
            </div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Name</TableHead>
                    <TableHead>Type</TableHead>
                    <TableHead>Unit Price</TableHead>
                    <TableHead>Qty on Hand</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead className="w-[50px]"></TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {products.map((product) => (
                    <TableRow key={product.id}>
                      <TableCell>
                        <div className="font-medium">{product.name}</div>
                        {product.description && <div className="text-sm text-muted-foreground">{product.description}</div>}
                      </TableCell>
                      <TableCell><Badge variant="outline">{product.type}</Badge></TableCell>
                      <TableCell>{formatCurrency(product.unitPrice)}</TableCell>
                      <TableCell>{product.trackQtyOnHand ? product.qtyOnHand : '-'}</TableCell>
                      <TableCell><Badge variant={product.active ? 'default' : 'secondary'}>{product.active ? 'Active' : 'Inactive'}</Badge></TableCell>
                      <TableCell>
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild><Button variant="ghost" size="icon"><MoreHorizontal className="h-4 w-4" /></Button></DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem onClick={() => handleOpenEditDialog(product)}><Edit className="h-4 w-4 mr-2" />Edit</DropdownMenuItem>
                            <DropdownMenuItem onClick={() => handleOpenDeleteDialog(product)} className="text-red-600"><Trash2 className="h-4 w-4 mr-2" />Delete</DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
          {totalCount > 0 && (
            <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 border-t px-4 py-3">
              <div className="flex items-center gap-4">
                <p className="text-sm text-muted-foreground">
                  Showing {(currentPage - 1) * currentPageSize + 1}–{Math.min(currentPage * currentPageSize, totalCount)} of {totalCount}
                </p>
                <div className="flex items-center gap-2">
                  <span className="text-sm text-muted-foreground whitespace-nowrap">Per page</span>
                  <Select value={String(pageSize)} onValueChange={(value) => setPageSize(Number(value))}>
                    <SelectTrigger className="w-[70px]" size="sm">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {PAGE_SIZE_OPTIONS.map((size) => (
                        <SelectItem key={size} value={String(size)}>{size}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
              <div className="flex items-center gap-2">
                <Button variant="outline" size="sm" onClick={() => goToPage(currentPage - 1)} disabled={!hasPreviousPage}>
                  <ChevronLeft className="h-4 w-4 mr-1" />
                  Previous
                </Button>
                <span className="text-sm text-muted-foreground">Page {currentPage} of {totalPages || 1}</span>
                <Button variant="outline" size="sm" onClick={() => goToPage(currentPage + 1)} disabled={!hasNextPage}>
                  Next
                  <ChevronRight className="h-4 w-4 ml-1" />
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog open={isCreateDialogOpen} onOpenChange={(open) => !open && dispatch(closeCreateDialog())}>
        <DialogContent className="max-w-lg max-h-[90vh] overflow-y-auto">
          <DialogHeader><DialogTitle>Add Product</DialogTitle><DialogDescription>Create a new product or service</DialogDescription></DialogHeader>
          <ProductForm onSubmit={handleCreate} onCancel={() => dispatch(closeCreateDialog())} isSubmitting={isSubmitting} />
        </DialogContent>
      </Dialog>

      <Dialog open={isEditDialogOpen} onOpenChange={(open) => !open && dispatch(closeEditDialog())}>
        <DialogContent className="max-w-lg max-h-[90vh] overflow-y-auto">
          <DialogHeader><DialogTitle>Edit Product</DialogTitle><DialogDescription>Update product information</DialogDescription></DialogHeader>
          {selectedProduct && <ProductForm product={selectedProduct} onSubmit={handleUpdate} onCancel={() => dispatch(closeEditDialog())} isSubmitting={isSubmitting} />}
        </DialogContent>
      </Dialog>

      <Dialog open={isDeleteDialogOpen} onOpenChange={(open) => !open && dispatch(closeDeleteDialog())}>
        <DialogContent>
          <DialogHeader><DialogTitle>Delete Product</DialogTitle><DialogDescription>Are you sure you want to delete {selectedProduct?.name}?</DialogDescription></DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => dispatch(closeDeleteDialog())}>Cancel</Button>
            <Button variant="destructive" onClick={handleDelete} disabled={isSubmitting}>{isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}Delete</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
