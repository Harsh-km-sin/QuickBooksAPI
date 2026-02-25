import { useState, useMemo, useEffect } from 'react';
import { useBills, useDebouncedValue } from '@/hooks';
import { useAppDispatch, useAppSelector } from '@/store/hooks';
import { openEditDialog, closeEditDialog, openDeleteDialog, closeDeleteDialog, setSubmitting } from '@/store/slices/billUiSlice';
import type { QBOBillHeader } from '@/types';
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
  RefreshCw,
  Loader2,
  FileText,
  Trash2,
  Edit,
  Calendar,
  DollarSign,
  Building2,
  ChevronLeft,
  ChevronRight,
  MoreHorizontal,
} from 'lucide-react';
import { Label } from '@/components/ui/label';

const SEARCH_DEBOUNCE_MS = 300;
const PAGE_SIZE_OPTIONS = [10, 20, 50, 100] as const;
const DEFAULT_PAGE_SIZE = 20;

export function Bills() {
  const [searchTerm, setSearchTerm] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);
  const debouncedSearch = useDebouncedValue(searchTerm.trim(), SEARCH_DEBOUNCE_MS);

  const listParams = useMemo(
    () => ({ page, pageSize, search: debouncedSearch || undefined }),
    [page, pageSize, debouncedSearch]
  );
  const {
    bills,
    totalCount,
    page: currentPage,
    pageSize: currentPageSize,
    totalPages,
    hasNextPage,
    hasPreviousPage,
    isLoading,
    isSyncing,
    getBillById,
    updateBill,
    deleteBill,
    sync,
  } = useBills({ listParams });

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, pageSize]);

  const goToPage = (nextPage: number) => setPage(() => Math.max(1, Math.min(nextPage, totalPages || 1)));

  const dispatch = useAppDispatch();
  const { isEditDialogOpen, isDeleteDialogOpen, selectedBill, isSubmitting } = useAppSelector((state) => state.billUi);
  const [isLoadingBill, setIsLoadingBill] = useState(false);

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

  const handleOpenDeleteDialog = (bill: QBOBillHeader) => {
    dispatch(openDeleteDialog(bill));
  };

  const formatCurrency = (value: number) => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);
  const formatDate = (dateString: string | null) => dateString ? new Date(dateString).toLocaleDateString() : '-';

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div><h1 className="text-3xl font-bold tracking-tight">Bills</h1><p className="text-muted-foreground">Manage your vendor bills</p></div>
          <Skeleton className="h-10 w-32" />
        </div>
        <Card><CardContent className="p-6"><Skeleton className="h-[400px] w-full" /></CardContent></Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div><h1 className="text-3xl font-bold tracking-tight">Bills</h1><p className="text-muted-foreground">Manage your vendor bills</p></div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={sync} disabled={isSyncing} className="hover:bg-muted hover:text-foreground">{isSyncing ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <RefreshCw className="h-4 w-4 mr-2" />}Sync</Button>
        </div>
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center gap-4">
            <div className="relative flex-1 max-w-sm"><Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" /><Input placeholder="Search bills..." value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} className="pl-9" /></div>
            <Badge variant="default">{totalCount} bill{totalCount !== 1 ? 's' : ''}</Badge>
          </div>
        </CardHeader>
        <CardContent>
          {bills.length === 0 ? (
            <div className="text-center py-12"><FileText className="h-12 w-12 text-muted-foreground mx-auto mb-4" /><h3 className="text-lg font-semibold mb-2">No bills found</h3><p className="text-muted-foreground mb-4">{debouncedSearch ? 'Try adjusting your search' : 'Sync with QuickBooks to import your bills'}</p>{!debouncedSearch && <Button onClick={sync} disabled={isSyncing}>{isSyncing ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <RefreshCw className="h-4 w-4 mr-2" />}Sync Bills</Button>}</div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow><TableHead>Vendor</TableHead><TableHead>Transaction Date</TableHead><TableHead>Due Date</TableHead><TableHead>Total Amount</TableHead><TableHead>Balance</TableHead><TableHead>Status</TableHead><TableHead className="w-[50px]"></TableHead></TableRow>
                </TableHeader>
                <TableBody>
                  {bills.map((bill) => (
                    <TableRow key={bill.billId}>
                      <TableCell><div className="flex items-center"><Building2 className="h-4 w-4 mr-2 text-muted-foreground" /><div className="font-medium">{bill.vendorRefName || 'Unknown Vendor'}</div></div></TableCell>
                      <TableCell><div className="flex items-center"><Calendar className="h-4 w-4 mr-1 text-muted-foreground" />{formatDate(bill.txnDate)}</div></TableCell>
                      <TableCell>{formatDate(bill.dueDate)}</TableCell>
                      <TableCell><div className="flex items-center font-medium"><DollarSign className="h-4 w-4 text-muted-foreground" />{formatCurrency(bill.totalAmt)}</div></TableCell>
                      <TableCell><span className={bill.balance > 0 ? 'text-destructive' : ''}>{formatCurrency(bill.balance)}</span></TableCell>
                      <TableCell><Badge variant={bill.balance === 0 ? 'default' : bill.balance < bill.totalAmt ? 'secondary' : 'destructive'}>{bill.balance === 0 ? 'Paid' : bill.balance < bill.totalAmt ? 'Partial' : 'Open'}</Badge></TableCell>
                      <TableCell>
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild><Button variant="ghost" size="icon"><MoreHorizontal className="h-4 w-4" /></Button></DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem onClick={() => handleOpenEditDialog(bill)} disabled={isLoadingBill}>
                              <Edit className="h-4 w-4 mr-2" />
                              Edit
                            </DropdownMenuItem>
                            <DropdownMenuItem onClick={() => handleOpenDeleteDialog(bill)} className="text-destructive"><Trash2 className="h-4 w-4 mr-2" />Delete</DropdownMenuItem>
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
