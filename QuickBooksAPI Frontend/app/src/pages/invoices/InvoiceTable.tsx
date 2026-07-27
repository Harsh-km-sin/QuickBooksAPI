import { Ban, Calendar, DollarSign, MoreHorizontal, Pencil, Receipt, Trash2, Users } from 'lucide-react';
import { useInvoicesList, useListQueryState } from '@/hooks';
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
import type { QBOInvoiceHeader } from '@/types';

export interface InvoiceTableProps {
  onOpenEdit: (invoice: QBOInvoiceHeader) => void;
  onOpenVoid: (invoice: QBOInvoiceHeader) => void;
  onOpenDelete: (invoice: QBOInvoiceHeader) => void;
}

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);
const formatDate = (dateString: string | null) => (dateString ? new Date(dateString).toLocaleDateString() : '-');

/**
 * Self-contained: owns search/sort/pagination state and the list query, so
 * paging/sorting only re-renders this subtree, not the whole Invoices page.
 */
export function InvoiceTable({ onOpenEdit, onOpenVoid, onOpenDelete }: InvoiceTableProps) {
  const {
    searchTerm,
    debouncedSearch,
    sortBy,
    sortDir,
    listParams,
    goToPage,
    handleSearchChange,
    handlePageSizeChange,
    handleSortChange,
  } = useListQueryState({ defaultSortBy: 'txnDate', defaultSortDir: 'desc' });

  const {
    invoices,
    totalCount,
    page,
    pageSize,
    totalPages,
    hasNextPage,
    hasPreviousPage,
    isLoading,
  } = useInvoicesList(listParams);

  const columns: DataTableColumn<QBOInvoiceHeader>[] = [
    {
      key: 'customerRefName',
      header: 'Customer',
      sortable: true,
      render: (invoice) => (
        <div className="flex items-center">
          <Users className="h-4 w-4 mr-2 text-muted-foreground" />
          <div className="font-medium">{invoice.customerRefName || 'Unknown Customer'}</div>
        </div>
      ),
    },
    {
      key: 'txnDate',
      header: 'Transaction Date',
      sortable: true,
      render: (invoice) => (
        <div className="flex items-center">
          <Calendar className="h-4 w-4 mr-1 text-muted-foreground" />
          {formatDate(invoice.txnDate)}
        </div>
      ),
    },
    {
      key: 'dueDate',
      header: 'Due Date',
      sortable: true,
      render: (invoice) => formatDate(invoice.dueDate),
    },
    {
      key: 'salesTermName',
      header: 'Terms',
      sortable: false,
      render: (invoice) => (
        <span className="text-xs font-medium text-muted-foreground">
          {invoice.salesTermName || '-'}
        </span>
      ),
    },
    {
      key: 'totalAmt',
      header: 'Total Amount',
      sortable: true,
      render: (invoice) => (
        <div className="flex items-center font-medium">
          <DollarSign className="h-4 w-4 text-muted-foreground" />
          {formatCurrency(invoice.totalAmt)}
        </div>
      ),
    },
    {
      key: 'balance',
      header: 'Balance',
      sortable: true,
      render: (invoice) => <span className={invoice.balance > 0 ? 'text-success' : ''}>{formatCurrency(invoice.balance)}</span>,
    },
    {
      key: 'status',
      header: 'Status',
      render: (invoice) => (
        <Badge variant={invoice.balance === 0 ? 'default' : invoice.balance < invoice.totalAmt ? 'secondary' : 'destructive'}>
          {invoice.balance === 0 ? 'Paid' : invoice.balance < invoice.totalAmt ? 'Partial' : 'Open'}
        </Badge>
      ),
    },
    {
      key: 'actions',
      header: '',
      headerClassName: 'w-[50px]',
      render: (invoice) => (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon">
              <MoreHorizontal className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => onOpenEdit(invoice)}>
              <Pencil className="h-4 w-4 mr-2" />
              Edit
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => onOpenVoid(invoice)}>
              <Ban className="h-4 w-4 mr-2" />
              Void
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => onOpenDelete(invoice)} className="text-destructive">
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
          placeholder="Search invoices..."
          rightSlot={
            <Badge variant="default">
              {totalCount} invoice{totalCount !== 1 ? 's' : ''}
            </Badge>
          }
        />
      </CardHeader>
      <CardContent className="flex min-h-0 flex-1 flex-col overflow-hidden px-6">
        <DataTable
          columns={columns}
          data={invoices}
          rowKey={(invoice) => invoice.invoiceId}
          isLoading={isLoading}
          sort={sortBy ? { sortBy, sortDir } : undefined}
          onSortChange={handleSortChange}
          emptyState={
            <div className="text-center py-12">
              <Receipt className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
              <h3 className="text-lg font-semibold mb-2">No invoices found</h3>
              <p className="text-muted-foreground mb-4">
                {debouncedSearch ? 'Try adjusting your search' : 'Sync with QuickBooks to import your invoices'}
              </p>
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
