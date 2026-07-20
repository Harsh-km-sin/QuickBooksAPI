import { BookOpen, DollarSign } from 'lucide-react';
import { useChartOfAccountsList, useListQueryState } from '@/hooks';
import { DataTable, DataTablePagination, SearchBar, type DataTableColumn } from '@/components/data-table';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import type { ChartOfAccounts } from '@/types';

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);

/**
 * Self-contained: owns search/sort/pagination state and the list query, so
 * paging/sorting only re-renders this subtree, not the whole ChartOfAccounts page.
 */
export function ChartOfAccountsTable() {
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
  } = useListQueryState({ defaultSortBy: 'name' });

  const {
    accounts,
    totalCount,
    page,
    pageSize,
    totalPages,
    hasNextPage,
    hasPreviousPage,
    isLoading,
  } = useChartOfAccountsList(listParams);

  const columns: DataTableColumn<ChartOfAccounts>[] = [
    {
      key: 'name',
      header: 'Name',
      sortable: true,
      render: (account) => (
        <>
          <div className="font-medium">{account.name}</div>
          {account.subAccount && <div className="text-xs text-muted-foreground">Sub-account</div>}
        </>
      ),
    },
    {
      key: 'accountType',
      header: 'Type',
      sortable: true,
      render: (account) => <Badge variant="outline">{account.accountType || 'Unknown'}</Badge>,
    },
    {
      key: 'classification',
      header: 'Classification',
      sortable: true,
      render: (account) => account.classification || '-',
    },
    {
      key: 'currentBalance',
      header: 'Current Balance',
      sortable: true,
      render: (account) => (
        <div className="flex items-center font-medium">
          <DollarSign className="h-4 w-4 text-muted-foreground" />
          {formatCurrency(account.currentBalance)}
        </div>
      ),
    },
    {
      key: 'active',
      header: 'Status',
      sortable: true,
      render: (account) => <Badge variant={account.active ? 'default' : 'secondary'}>{account.active ? 'Active' : 'Inactive'}</Badge>,
    },
  ];

  return (
    <Card className="flex min-h-0 flex-1 flex-col overflow-hidden">
      <CardHeader>
        <SearchBar
          value={searchTerm}
          onChange={handleSearchChange}
          placeholder="Search accounts..."
          rightSlot={
            <Badge variant="default">
              {totalCount} account{totalCount !== 1 ? 's' : ''}
            </Badge>
          }
        />
      </CardHeader>
      <CardContent className="flex min-h-0 flex-1 flex-col overflow-hidden px-6">
        <DataTable
          columns={columns}
          data={accounts}
          rowKey={(account) => account.id}
          isLoading={isLoading}
          sort={sortBy ? { sortBy, sortDir } : undefined}
          onSortChange={handleSortChange}
          emptyState={
            <div className="text-center py-12">
              <BookOpen className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
              <h3 className="text-lg font-semibold mb-2">No accounts found</h3>
              <p className="text-muted-foreground mb-4">
                {debouncedSearch ? 'Try adjusting your search' : 'Sync with QuickBooks to import your chart of accounts'}
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
