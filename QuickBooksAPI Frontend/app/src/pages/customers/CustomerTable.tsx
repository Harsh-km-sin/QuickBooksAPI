import { Mail, MapPin, MoreHorizontal, Edit, Trash2, Phone, Plus, Users } from 'lucide-react';
import { useCustomersList, useListQueryState } from '@/hooks';
import { DataTable, DataTablePagination, SearchBar, type DataTableColumn } from '@/components/data-table';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import type { Customer } from '@/types';

export interface CustomerTableProps {
  onOpenCreate: () => void;
  onOpenEdit: (customer: Customer) => void;
  onOpenDelete: (customer: Customer) => void;
  isLoadingCustomer: boolean;
}

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);

/**
 * Self-contained: owns search/sort/pagination state and the list query, so
 * paging/sorting only re-renders this subtree, not the whole Customers page.
 */
export function CustomerTable({ onOpenCreate, onOpenEdit, onOpenDelete, isLoadingCustomer }: CustomerTableProps) {
  const {
    searchTerm,
    debouncedSearch,
    activeFilter,
    sortBy,
    sortDir,
    listParams,
    goToPage,
    handleSearchChange,
    handlePageSizeChange,
    handleActiveFilterChange,
    handleSortChange,
  } = useListQueryState({ defaultSortBy: 'displayName' });

  const {
    customers,
    totalCount,
    page,
    pageSize,
    totalPages,
    hasNextPage,
    hasPreviousPage,
    isLoading,
  } = useCustomersList(listParams);

  const columns: DataTableColumn<Customer>[] = [
    {
      key: 'displayName',
      header: 'Name',
      sortable: true,
      render: (customer) => (
        <>
          <div className="font-medium">{customer.displayName || `${customer.givenName} ${customer.familyName}`}</div>
          {customer.displayName && customer.givenName && (
            <div className="text-sm text-muted-foreground">
              {customer.givenName} {customer.familyName}
            </div>
          )}
        </>
      ),
    },
    {
      key: 'companyName',
      header: 'Company',
      sortable: true,
      render: (customer) => customer.companyName || '-',
    },
    {
      key: 'contact',
      header: 'Contact',
      render: (customer) => (
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
      ),
    },
    {
      key: 'balance',
      header: 'Balance',
      sortable: true,
      render: (customer) => (
        <span className={customer.balance > 0 ? 'text-destructive' : ''}>{formatCurrency(customer.balance)}</span>
      ),
    },
    {
      key: 'active',
      header: 'Status',
      sortable: true,
      render: (customer) => <Badge variant={customer.active ? 'default' : 'secondary'}>{customer.active ? 'Active' : 'Inactive'}</Badge>,
    },
    {
      key: 'actions',
      header: '',
      headerClassName: 'w-[50px]',
      render: (customer) => (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon">
              <MoreHorizontal className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => onOpenEdit(customer)} disabled={isLoadingCustomer}>
              <Edit className="h-4 w-4 mr-2" />
              Edit
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => onOpenDelete(customer)} className="text-destructive">
              <Trash2 className="h-4 w-4 mr-2" />
              Delete
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      ),
    },
  ];

  return (
    <Card className="flex min-h-0 flex-1 flex-col overflow-hidden">
      <CardHeader>
        <SearchBar
          value={searchTerm}
          onChange={handleSearchChange}
          placeholder="Search customers..."
          rightSlot={
            <>
              <Select value={activeFilter} onValueChange={(value) => handleActiveFilterChange(value as 'active' | 'inactive' | 'all')}>
                <SelectTrigger className="w-[140px]">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="active">Active</SelectItem>
                  <SelectItem value="inactive">Inactive</SelectItem>
                  <SelectItem value="all">All</SelectItem>
                </SelectContent>
              </Select>
              <Badge variant="default">
                {totalCount} customer{totalCount !== 1 ? 's' : ''}
              </Badge>
            </>
          }
        />
      </CardHeader>
      <CardContent className="flex min-h-0 flex-1 flex-col overflow-hidden px-6">
        <DataTable
          columns={columns}
          data={customers}
          rowKey={(customer) => customer.id}
          isLoading={isLoading}
          sort={sortBy ? { sortBy, sortDir } : undefined}
          onSortChange={handleSortChange}
          emptyState={
            <div className="text-center py-12">
              <Users className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
              <h3 className="text-lg font-semibold mb-2">No customers found</h3>
              <p className="text-muted-foreground mb-4">
                {debouncedSearch ? 'Try adjusting your search' : 'Get started by adding your first customer'}
              </p>
              {!debouncedSearch && (
                <Button onClick={onOpenCreate}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add Customer
                </Button>
              )}
            </div>
          }
        />
        <DataTablePagination
          currentPage={page}
          pageSize={pageSize}
          totalCount={totalCount}
          totalPages={totalPages}
          hasNextPage={hasNextPage}
          hasPreviousPage={hasPreviousPage}
          onPageChange={goToPage}
          onPageSizeChange={handlePageSizeChange}
        />
      </CardContent>
    </Card>
  );
}
