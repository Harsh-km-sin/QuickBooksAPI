import { Mail, MapPin, MoreHorizontal, Edit, Trash2, Phone, Plus, Truck } from 'lucide-react';
import { useVendorsList, useListQueryState } from '@/hooks';
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
import type { Vendor } from '@/types';

export interface VendorTableProps {
  onOpenCreate: () => void;
  onOpenEdit: (vendor: Vendor) => void;
  onOpenDelete: (vendor: Vendor) => void;
}

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);

/**
 * Self-contained: owns search/sort/pagination state and the list query, so
 * paging/sorting only re-renders this subtree, not the whole Vendors page.
 */
export function VendorTable({ onOpenCreate, onOpenEdit, onOpenDelete }: VendorTableProps) {
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
    vendors,
    totalCount,
    page,
    pageSize,
    totalPages,
    hasNextPage,
    hasPreviousPage,
    isLoading,
  } = useVendorsList(listParams);

  const columns: DataTableColumn<Vendor>[] = [
    {
      key: 'displayName',
      header: 'Name',
      sortable: true,
      render: (vendor) => <div className="font-medium">{vendor.displayName || `${vendor.givenName} ${vendor.familyName}`}</div>,
    },
    {
      key: 'companyName',
      header: 'Company',
      sortable: true,
      render: (vendor) => vendor.companyName || '-',
    },
    {
      key: 'contact',
      header: 'Contact',
      render: (vendor) => (
        <div className="space-y-1">
          {vendor.primaryEmailAddr && (
            <div className="flex items-center text-sm">
              <Mail className="h-3 w-3 mr-1 text-muted-foreground" />
              {vendor.primaryEmailAddr}
            </div>
          )}
          {vendor.primaryPhone && (
            <div className="flex items-center text-sm">
              <Phone className="h-3 w-3 mr-1 text-muted-foreground" />
              {vendor.primaryPhone}
            </div>
          )}
          {vendor.billAddrLine1 && (
            <div className="flex items-center text-sm text-muted-foreground">
              <MapPin className="h-3 w-3 mr-1" />
              {vendor.billAddrCity}, {vendor.billAddrCountrySubDivisionCode}
            </div>
          )}
        </div>
      ),
    },
    {
      key: 'balance',
      header: 'Balance',
      sortable: true,
      render: (vendor) => (
        <span className={vendor.balance > 0 ? 'text-destructive' : ''}>{formatCurrency(vendor.balance)}</span>
      ),
    },
    {
      key: 'active',
      header: 'Status',
      sortable: true,
      render: (vendor) => <Badge variant={vendor.active ? 'default' : 'secondary'}>{vendor.active ? 'Active' : 'Inactive'}</Badge>,
    },
    {
      key: 'actions',
      header: '',
      headerClassName: 'w-[50px]',
      render: (vendor) => (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon">
              <MoreHorizontal className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => onOpenEdit(vendor)}>
              <Edit className="h-4 w-4 mr-2" />
              Edit
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => onOpenDelete(vendor)} className="text-destructive">
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
          placeholder="Search vendors..."
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
                {totalCount} vendor{totalCount !== 1 ? 's' : ''}
              </Badge>
            </>
          }
        />
      </CardHeader>
      <CardContent className="flex min-h-0 flex-1 flex-col overflow-hidden px-6">
        <DataTable
          columns={columns}
          data={vendors}
          rowKey={(vendor) => vendor.qboId}
          isLoading={isLoading}
          sort={sortBy ? { sortBy, sortDir } : undefined}
          onSortChange={handleSortChange}
          emptyState={
            <div className="text-center py-12">
              <Truck className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
              <h3 className="text-lg font-semibold mb-2">No vendors found</h3>
              <p className="text-muted-foreground mb-4">
                {debouncedSearch ? 'Try adjusting your search' : 'Get started by adding your first vendor'}
              </p>
              {!debouncedSearch && (
                <Button onClick={onOpenCreate}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add Vendor
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
