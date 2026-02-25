import { useState, useMemo, useEffect } from 'react';
import { useInvoices, useDebouncedValue } from '@/hooks';
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
  Receipt,
  Trash2,
  Calendar,
  DollarSign,
  Users,
  Ban,
  ChevronLeft,
  ChevronRight,
  MoreHorizontal,
} from 'lucide-react';

const SEARCH_DEBOUNCE_MS = 300;
const PAGE_SIZE_OPTIONS = [10, 20, 50, 100] as const;
const DEFAULT_PAGE_SIZE = 20;

export function Invoices() {
  const [searchTerm, setSearchTerm] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);
  const debouncedSearch = useDebouncedValue(searchTerm.trim(), SEARCH_DEBOUNCE_MS);

  const listParams = useMemo(
    () => ({ page, pageSize, search: debouncedSearch || undefined }),
    [page, pageSize, debouncedSearch]
  );
  const {
    invoices,
    totalCount,
    page: currentPage,
    pageSize: currentPageSize,
    totalPages,
    hasNextPage,
    hasPreviousPage,
    isLoading,
    isSyncing,
    deleteInvoice,
    voidInvoice,
    sync,
  } = useInvoices({ listParams });

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, pageSize]);

  const goToPage = (nextPage: number) => setPage(() => Math.max(1, Math.min(nextPage, totalPages || 1)));

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

  const handleOpenDeleteDialog = (invoice: QBOInvoiceHeader) => {
    dispatch(openDeleteDialog(invoice));
  };

  const handleOpenVoidDialog = (invoice: QBOInvoiceHeader) => {
    dispatch(openVoidDialog(invoice));
  };

  const formatCurrency = (value: number) => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);
  const formatDate = (dateString: string | null) => dateString ? new Date(dateString).toLocaleDateString() : '-';

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div><h1 className="text-3xl font-bold tracking-tight">Invoices</h1><p className="text-muted-foreground">Manage your customer invoices</p></div>
          <Skeleton className="h-10 w-32" />
        </div>
        <Card><CardContent className="p-6"><Skeleton className="h-[400px] w-full" /></CardContent></Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div><h1 className="text-3xl font-bold tracking-tight">Invoices</h1><p className="text-muted-foreground">Manage your customer invoices</p></div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={sync} disabled={isSyncing} className="hover:bg-muted hover:text-foreground">{isSyncing ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <RefreshCw className="h-4 w-4 mr-2" />}Sync</Button>
        </div>
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center gap-4">
            <div className="relative flex-1 max-w-sm"><Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" /><Input placeholder="Search invoices..." value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} className="pl-9" /></div>
            <Badge variant="default">{totalCount} invoice{totalCount !== 1 ? 's' : ''}</Badge>
          </div>
        </CardHeader>
        <CardContent>
          {invoices.length === 0 ? (
            <div className="text-center py-12"><Receipt className="h-12 w-12 text-muted-foreground mx-auto mb-4" /><h3 className="text-lg font-semibold mb-2">No invoices found</h3><p className="text-muted-foreground mb-4">{debouncedSearch ? 'Try adjusting your search' : 'Sync with QuickBooks to import your invoices'}</p>{!debouncedSearch && <Button onClick={sync} disabled={isSyncing}>{isSyncing ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <RefreshCw className="h-4 w-4 mr-2" />}Sync Invoices</Button>}</div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow><TableHead>Customer</TableHead><TableHead>Transaction Date</TableHead><TableHead>Due Date</TableHead><TableHead>Total Amount</TableHead><TableHead>Balance</TableHead><TableHead>Status</TableHead><TableHead className="w-[50px]"></TableHead></TableRow>
                </TableHeader>
                <TableBody>
                  {invoices.map((invoice) => (
                    <TableRow key={invoice.invoiceId}>
                      <TableCell><div className="flex items-center"><Users className="h-4 w-4 mr-2 text-muted-foreground" /><div className="font-medium">{invoice.customerRefName || 'Unknown Customer'}</div></div></TableCell>
                      <TableCell><div className="flex items-center"><Calendar className="h-4 w-4 mr-1 text-muted-foreground" />{formatDate(invoice.txnDate)}</div></TableCell>
                      <TableCell>{formatDate(invoice.dueDate)}</TableCell>
                      <TableCell><div className="flex items-center font-medium"><DollarSign className="h-4 w-4 text-muted-foreground" />{formatCurrency(invoice.totalAmt)}</div></TableCell>
                      <TableCell><span className={invoice.balance > 0 ? 'text-success' : ''}>{formatCurrency(invoice.balance)}</span></TableCell>
                      <TableCell><Badge variant={invoice.balance === 0 ? 'default' : invoice.balance < invoice.totalAmt ? 'secondary' : 'destructive'}>{invoice.balance === 0 ? 'Paid' : invoice.balance < invoice.totalAmt ? 'Partial' : 'Open'}</Badge></TableCell>
                      <TableCell>
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild><Button variant="ghost" size="icon"><MoreHorizontal className="h-4 w-4" /></Button></DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem onClick={() => handleOpenVoidDialog(invoice)}><Ban className="h-4 w-4 mr-2" />Void</DropdownMenuItem>
                            <DropdownMenuItem onClick={() => handleOpenDeleteDialog(invoice)} className="text-destructive"><Trash2 className="h-4 w-4 mr-2" />Delete</DropdownMenuItem>
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
