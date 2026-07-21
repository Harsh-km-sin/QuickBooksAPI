import { MoreHorizontal, Edit, Trash2, Plus, Package } from 'lucide-react';
import { useProductsList, useListQueryState } from '@/hooks';
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
import type { Products } from '@/types';

export interface ProductTableProps {
  onOpenCreate: () => void;
  onOpenEdit: (product: Products) => void;
  onOpenDelete: (product: Products) => void;
}

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);

/**
 * Self-contained: owns search/sort/pagination state and the list query, so
 * paging/sorting only re-renders this subtree, not the whole Products page.
 */
export function ProductTable({ onOpenCreate, onOpenEdit, onOpenDelete }: ProductTableProps) {
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
  } = useListQueryState({ defaultSortBy: 'name' });

  const {
    products,
    totalCount,
    page,
    pageSize,
    totalPages,
    hasNextPage,
    hasPreviousPage,
    isLoading,
  } = useProductsList(listParams);

  const columns: DataTableColumn<Products>[] = [
    {
      key: 'name',
      header: 'Name',
      sortable: true,
      render: (product) => (
        <>
          <div className="font-medium">{product.name}</div>
          {product.description && <div className="text-sm text-muted-foreground">{product.description}</div>}
        </>
      ),
    },
    {
      key: 'type',
      header: 'Type',
      sortable: true,
      render: (product) => <Badge variant="outline">{product.type}</Badge>,
    },
    {
      key: 'unitPrice',
      header: 'Unit Price',
      sortable: true,
      render: (product) => formatCurrency(product.unitPrice),
    },
    {
      key: 'qtyOnHand',
      header: 'Qty on Hand',
      sortable: true,
      render: (product) => (product.trackQtyOnHand ? product.qtyOnHand : '-'),
    },
    {
      key: 'active',
      header: 'Status',
      sortable: true,
      render: (product) => <Badge variant={product.active ? 'default' : 'secondary'}>{product.active ? 'Active' : 'Inactive'}</Badge>,
    },
    {
      key: 'actions',
      header: '',
      headerClassName: 'w-[50px]',
      render: (product) => (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon">
              <MoreHorizontal className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => onOpenEdit(product)}>
              <Edit className="h-4 w-4 mr-2" />
              Edit
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => onOpenDelete(product)} className="text-destructive">
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
          placeholder="Search products..."
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
                {totalCount} product{totalCount !== 1 ? 's' : ''}
              </Badge>
            </>
          }
        />
      </CardHeader>
      <CardContent className="flex min-h-0 flex-1 flex-col overflow-hidden px-6">
        <DataTable
          columns={columns}
          data={products}
          rowKey={(product) => product.id}
          isLoading={isLoading}
          sort={sortBy ? { sortBy, sortDir } : undefined}
          onSortChange={handleSortChange}
          emptyState={
            <div className="text-center py-12">
              <Package className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
              <h3 className="text-lg font-semibold mb-2">No products found</h3>
              <p className="text-muted-foreground mb-4">
                {debouncedSearch ? 'Try adjusting your search' : 'Get started by adding your first product'}
              </p>
              {!debouncedSearch && (
                <Button onClick={onOpenCreate}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add Product
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
