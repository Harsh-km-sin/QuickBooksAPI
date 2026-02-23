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
  Mail,
  Phone,
  MapPin,
  Users,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react';

const SEARCH_DEBOUNCE_MS = 300;
const PAGE_SIZE_OPTIONS = [10, 20, 50, 100] as const;
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

  const handleOpenDeleteDialog = (customer: Customer) => {
    dispatch(openDeleteDialog(customer));
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

      <Card>
        <CardHeader>
          <div className="flex items-center gap-4">
            <div className="relative flex-1 max-w-sm">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search customers..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-9"
              />
            </div>
            <Select value={activeFilter} onValueChange={(value) => setActiveFilter(value as 'active' | 'inactive' | 'all')}>
              <SelectTrigger className="w-[140px]">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="active">Active</SelectItem>
                <SelectItem value="inactive">Inactive</SelectItem>
                <SelectItem value="all">All</SelectItem>
              </SelectContent>
            </Select>
            <Badge variant="secondary">
              {totalCount} customer{totalCount !== 1 ? 's' : ''}
            </Badge>
          </div>
        </CardHeader>
        <CardContent>
          {customers.length === 0 ? (
            <div className="text-center py-12">
              <Users className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
              <h3 className="text-lg font-semibold mb-2">No customers found</h3>
              <p className="text-muted-foreground mb-4">
                {debouncedSearch ? 'Try adjusting your search' : 'Get started by adding your first customer'}
              </p>
              {!debouncedSearch && (
                <Button onClick={() => dispatch(openCreateDialog())}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add Customer
                </Button>
              )}
            </div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Name</TableHead>
                    <TableHead>Company</TableHead>
                    <TableHead>Contact</TableHead>
                    <TableHead>Balance</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead className="w-[50px]"></TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {customers.map((customer) => (
                    <TableRow key={customer.id}>
                      <TableCell>
                        <div className="font-medium">{customer.displayName || `${customer.givenName} ${customer.familyName}`}</div>
                        {customer.displayName && customer.givenName && (
                          <div className="text-sm text-muted-foreground">
                            {customer.givenName} {customer.familyName}
                          </div>
                        )}
                      </TableCell>
                      <TableCell>{customer.companyName || '-'}</TableCell>
                      <TableCell>
                        <div className="space-y-1">
                          {customer.primaryEmailAddr && (
                            <div className="flex items-center text-sm">
                              <Mail className="h-3 w-3 mr-1 text-muted-foreground" />
                              {customer.primaryEmailAddr}
                            </div>
                          )}
                          {customer.primaryPhone && (
                            <div className="flex items-center text-sm">
                              <Phone className="h-3 w-3 mr-1 text-muted-foreground" />
                              {customer.primaryPhone}
                            </div>
                          )}
                          {customer.billAddrLine1 && (
                            <div className="flex items-center text-sm text-muted-foreground">
                              <MapPin className="h-3 w-3 mr-1" />
                              {customer.billAddrCity}, {customer.billAddrCountrySubDivisionCode}
                            </div>
                          )}
                        </div>
                      </TableCell>
                      <TableCell>
                        <span className={customer.balance > 0 ? 'text-red-600' : ''}>
                          {formatCurrency(customer.balance)}
                        </span>
                      </TableCell>
                      <TableCell>
                        <Badge variant={customer.active ? 'default' : 'secondary'}>
                          {customer.active ? 'Active' : 'Inactive'}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button variant="ghost" size="icon">
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem onClick={() => handleOpenEditDialog(customer)} disabled={isLoadingCustomer}>
                              <Edit className="h-4 w-4 mr-2" />
                              Edit
                            </DropdownMenuItem>
                            <DropdownMenuItem 
                              onClick={() => handleOpenDeleteDialog(customer)}
                              className="text-red-600"
                            >
                              <Trash2 className="h-4 w-4 mr-2" />
                              Delete
                            </DropdownMenuItem>
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
                  Showing {(currentPage - 1) * pageSize + 1}–{Math.min(currentPage * pageSize, totalCount)} of {totalCount}
                </p>
                <div className="flex items-center gap-2">
                  <span className="text-sm text-muted-foreground whitespace-nowrap">Per page</span>
                  <Select
                    value={String(pageSize)}
                    onValueChange={(value) => setPageSize(Number(value))}
                  >
                    <SelectTrigger className="w-[70px]" size="sm">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {PAGE_SIZE_OPTIONS.map((size) => (
                        <SelectItem key={size} value={String(size)}>
                          {size}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => goToPage(currentPage - 1)}
                  disabled={!hasPreviousPage}
                >
                  <ChevronLeft className="h-4 w-4 mr-1" />
                  Previous
                </Button>
                <span className="text-sm text-muted-foreground">
                  Page {currentPage} of {totalPages || 1}
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => goToPage(currentPage + 1)}
                  disabled={!hasNextPage}
                >
                  Next
                  <ChevronRight className="h-4 w-4 ml-1" />
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

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
