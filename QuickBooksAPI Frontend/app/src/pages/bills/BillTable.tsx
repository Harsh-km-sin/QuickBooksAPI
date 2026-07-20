import { Building2, Calendar, DollarSign, Edit, FileText, MoreHorizontal, Trash2 } from 'lucide-react';
import { useBillsList, useListQueryState } from '@/hooks';
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
import type { QBOBillHeader } from '@/types';

export interface BillTableProps {
  onOpenEdit: (bill: QBOBillHeader) => void;
  onOpenDelete: (bill: QBOBillHeader) => void;
  isLoadingBill: boolean;
}

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);
const formatDate = (dateString: string | null) => (dateString ? new Date(dateString).toLocaleDateString() : '-');

/**
 * Self-contained: owns search/sort/pagination state and the list query, so
 * paging/sorting only re-renders this subtree, not the whole Bills page.
 */
export function BillTable({ onOpenEdit, onOpenDelete, isLoadingBill }: BillTableProps) {
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
    bills,
    totalCount,
    page,
    pageSize,
    totalPages,
    hasNextPage,
    hasPreviousPage,
    isLoading,
  } = useBillsList(listParams);

  const columns: DataTableColumn<QBOBillHeader>[] = [
    {
      key: 'vendorRefName',
      header: 'Vendor',
      sortable: true,
      render: (bill) => (
        <div className="flex items-center">
          <Building2 className="h-4 w-4 mr-2 text-muted-foreground" />
          <div className="font-medium">{bill.vendorRefName || 'Unknown Vendor'}</div>
        </div>
      ),
    },
    {
      key: 'txnDate',
      header: 'Transaction Date',
      sortable: true,
      render: (bill) => (
        <div className="flex items-center">
          <Calendar className="h-4 w-4 mr-1 text-muted-foreground" />
          {formatDate(bill.txnDate)}
        </div>
      ),
    },
    {
      key: 'dueDate',
      header: 'Due Date',
      sortable: true,
      render: (bill) => formatDate(bill.dueDate),
    },
    {
      key: 'totalAmt',
      header: 'Total Amount',
      sortable: true,
      render: (bill) => (
        <div className="flex items-center font-medium">
          <DollarSign className="h-4 w-4 text-muted-foreground" />
          {formatCurrency(bill.totalAmt)}
        </div>
      ),
    },
    {
      key: 'balance',
      header: 'Balance',
      sortable: true,
      render: (bill) => <span className={bill.balance > 0 ? 'text-destructive' : ''}>{formatCurrency(bill.balance)}</span>,
    },
    {
      key: 'status',
      header: 'Status',
      render: (bill) => (
        <Badge variant={bill.balance === 0 ? 'default' : bill.balance < bill.totalAmt ? 'secondary' : 'destructive'}>
          {bill.balance === 0 ? 'Paid' : bill.balance < bill.totalAmt ? 'Partial' : 'Open'}
        </Badge>
      ),
    },
    {
      key: 'actions',
      header: '',
      headerClassName: 'w-[50px]',
      render: (bill) => (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon">
              <MoreHorizontal className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => onOpenEdit(bill)} disabled={isLoadingBill}>
              <Edit className="h-4 w-4 mr-2" />
              Edit
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => onOpenDelete(bill)} className="text-destructive">
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
          placeholder="Search bills..."
          rightSlot={
            <Badge variant="default">
              {totalCount} bill{totalCount !== 1 ? 's' : ''}
            </Badge>
          }
        />
      </CardHeader>
      <CardContent className="flex min-h-0 flex-1 flex-col overflow-hidden px-6">
        <DataTable
          columns={columns}
          data={bills}
          rowKey={(bill) => bill.billId}
          isLoading={isLoading}
          sort={sortBy ? { sortBy, sortDir } : undefined}
          onSortChange={handleSortChange}
          emptyState={
            <div className="text-center py-12">
              <FileText className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
              <h3 className="text-lg font-semibold mb-2">No bills found</h3>
              <p className="text-muted-foreground mb-4">
                {debouncedSearch ? 'Try adjusting your search' : 'Sync with QuickBooks to import your bills'}
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
