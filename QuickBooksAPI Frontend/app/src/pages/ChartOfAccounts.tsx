import { useState, useMemo, useEffect } from 'react';
import { useChartOfAccounts, useDebouncedValue } from '@/hooks';
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
  BookOpen,
  DollarSign,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react';

const SEARCH_DEBOUNCE_MS = 300;
const PAGE_SIZE_OPTIONS = [10, 20, 50, 100] as const;
const DEFAULT_PAGE_SIZE = 20;

export function ChartOfAccounts() {
  const [searchTerm, setSearchTerm] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);
  const debouncedSearch = useDebouncedValue(searchTerm.trim(), SEARCH_DEBOUNCE_MS);

  const listParams = useMemo(
    () => ({ page, pageSize, search: debouncedSearch || undefined }),
    [page, pageSize, debouncedSearch]
  );
  const {
    accounts,
    totalCount,
    page: currentPage,
    pageSize: currentPageSize,
    totalPages,
    hasNextPage,
    hasPreviousPage,
    isLoading,
    isSyncing,
    sync,
  } = useChartOfAccounts({ listParams });

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, pageSize]);

  const goToPage = (nextPage: number) => setPage(() => Math.max(1, Math.min(nextPage, totalPages || 1)));

  const formatCurrency = (value: number) => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div><h1 className="text-3xl font-bold tracking-tight">Chart of Accounts</h1><p className="text-muted-foreground">View your chart of accounts</p></div>
          <Skeleton className="h-10 w-32" />
        </div>
        <Card><CardContent className="p-6"><Skeleton className="h-[400px] w-full" /></CardContent></Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div><h1 className="text-3xl font-bold tracking-tight">Chart of Accounts</h1><p className="text-muted-foreground">View your chart of accounts</p></div>
        <Button variant="outline" onClick={sync} disabled={isSyncing}>{isSyncing ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <RefreshCw className="h-4 w-4 mr-2" />}Sync</Button>
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center gap-4">
            <div className="relative flex-1 max-w-sm"><Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" /><Input placeholder="Search accounts..." value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} className="pl-9" /></div>
            <Badge variant="secondary">{totalCount} account{totalCount !== 1 ? 's' : ''}</Badge>
          </div>
        </CardHeader>
        <CardContent>
          {accounts.length === 0 ? (
            <div className="text-center py-12"><BookOpen className="h-12 w-12 text-muted-foreground mx-auto mb-4" /><h3 className="text-lg font-semibold mb-2">No accounts found</h3><p className="text-muted-foreground mb-4">{debouncedSearch ? 'Try adjusting your search' : 'Sync with QuickBooks to import your chart of accounts'}</p>{!debouncedSearch && <Button onClick={sync} disabled={isSyncing}>{isSyncing ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <RefreshCw className="h-4 w-4 mr-2" />}Sync Accounts</Button>}</div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow><TableHead>Name</TableHead><TableHead>Type</TableHead><TableHead>Classification</TableHead><TableHead>Current Balance</TableHead><TableHead>Status</TableHead></TableRow>
                </TableHeader>
                <TableBody>
                  {accounts.map((account) => (
                    <TableRow key={account.id}>
                      <TableCell>
                        <div className="font-medium">{account.name}</div>
                        {account.subAccount && <div className="text-xs text-muted-foreground">Sub-account</div>}
                      </TableCell>
                      <TableCell><Badge variant="outline">{account.accountType || 'Unknown'}</Badge></TableCell>
                      <TableCell>{account.classification || '-'}</TableCell>
                      <TableCell><div className="flex items-center font-medium"><DollarSign className="h-4 w-4 text-muted-foreground" />{formatCurrency(account.currentBalance)}</div></TableCell>
                      <TableCell><Badge variant={account.active ? 'default' : 'secondary'}>{account.active ? 'Active' : 'Inactive'}</Badge></TableCell>
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
    </div>
  );
}
