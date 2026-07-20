import { BookText, Calendar, DollarSign, FileText } from 'lucide-react';
import { useJournalEntriesList, useListQueryState } from '@/hooks';
import { DataTable, DataTablePagination, SearchBar, type DataTableColumn } from '@/components/data-table';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import type { QBOJournalEntryHeader } from '@/types';

const formatCurrency = (value: number | null) =>
  value != null ? new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value) : '-';
const formatDate = (dateString: string | null) => (dateString ? new Date(dateString).toLocaleDateString() : '-');

/**
 * Self-contained: owns search/sort/pagination state and the list query, so
 * paging/sorting only re-renders this subtree, not the whole JournalEntries page.
 */
export function JournalEntryTable() {
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
    entries,
    totalCount,
    page,
    pageSize,
    totalPages,
    hasNextPage,
    hasPreviousPage,
    isLoading,
  } = useJournalEntriesList(listParams);

  const columns: DataTableColumn<QBOJournalEntryHeader>[] = [
    {
      key: 'docNumber',
      header: 'Doc #',
      sortable: true,
      render: (entry) => (
        <div className="flex items-center">
          <FileText className="h-4 w-4 mr-2 text-muted-foreground" />
          <div className="font-medium">{entry.docNumber || '-'}</div>
        </div>
      ),
    },
    {
      key: 'txnDate',
      header: 'Date',
      sortable: true,
      render: (entry) => (
        <div className="flex items-center">
          <Calendar className="h-4 w-4 mr-1 text-muted-foreground" />
          {formatDate(entry.txnDate)}
        </div>
      ),
    },
    {
      key: 'totalAmount',
      header: 'Total Amount',
      sortable: true,
      render: (entry) => (
        <div className="flex items-center font-medium">
          <DollarSign className="h-4 w-4 text-muted-foreground" />
          {formatCurrency(entry.totalAmount)}
        </div>
      ),
    },
    {
      key: 'adjustment',
      header: 'Adjustment',
      sortable: true,
      render: (entry) => <Badge variant={entry.adjustment ? 'default' : 'outline'}>{entry.adjustment ? 'Yes' : 'No'}</Badge>,
    },
    {
      key: 'note',
      header: 'Note',
      render: (entry) => <div className="max-w-xs truncate text-muted-foreground">{entry.privateNote || '-'}</div>,
    },
  ];

  return (
    <Card className="flex min-h-0 flex-1 flex-col overflow-hidden">
      <CardHeader>
        <SearchBar
          value={searchTerm}
          onChange={handleSearchChange}
          placeholder="Search journal entries..."
          rightSlot={
            <Badge variant="default">
              {totalCount} entr{totalCount !== 1 ? 'ies' : 'y'}
            </Badge>
          }
        />
      </CardHeader>
      <CardContent className="flex min-h-0 flex-1 flex-col overflow-hidden px-6">
        <DataTable
          columns={columns}
          data={entries}
          rowKey={(entry) => entry.journalEntryId}
          isLoading={isLoading}
          sort={sortBy ? { sortBy, sortDir } : undefined}
          onSortChange={handleSortChange}
          emptyState={
            <div className="text-center py-12">
              <BookText className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
              <h3 className="text-lg font-semibold mb-2">No journal entries found</h3>
              <p className="text-muted-foreground mb-4">
                {debouncedSearch ? 'Try adjusting your search' : 'Sync with QuickBooks to import your journal entries'}
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
