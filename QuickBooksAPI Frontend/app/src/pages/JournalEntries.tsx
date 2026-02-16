import { useState } from 'react';
import { useJournalEntries } from '@/hooks/useJournalEntries';
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
  Search,
  RefreshCw,
  Loader2,
  BookText,
  Calendar,
  DollarSign,
  FileText,
} from 'lucide-react';

export function JournalEntries() {
  const { entries, isLoading, isSyncing, sync } = useJournalEntries();
  const [searchTerm, setSearchTerm] = useState('');

  const filteredEntries = entries.filter((entry) => {
    const searchLower = searchTerm.toLowerCase();
    return entry.docNumber?.toLowerCase().includes(searchLower) || entry.privateNote?.toLowerCase().includes(searchLower);
  });

  const formatCurrency = (value: number | null) => value != null ? new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value) : '-';
  const formatDate = (dateString: string | null) => dateString ? new Date(dateString).toLocaleDateString() : '-';

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div><h1 className="text-3xl font-bold tracking-tight">Journal Entries</h1><p className="text-muted-foreground">View your journal entries</p></div>
          <Skeleton className="h-10 w-32" />
        </div>
        <Card><CardContent className="p-6"><Skeleton className="h-[400px] w-full" /></CardContent></Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div><h1 className="text-3xl font-bold tracking-tight">Journal Entries</h1><p className="text-muted-foreground">View your journal entries</p></div>
        <Button variant="outline" onClick={sync} disabled={isSyncing}>{isSyncing ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <RefreshCw className="h-4 w-4 mr-2" />}Sync</Button>
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center gap-4">
            <div className="relative flex-1 max-w-sm"><Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" /><Input placeholder="Search journal entries..." value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} className="pl-9" /></div>
            <Badge variant="secondary">{filteredEntries.length} entr{filteredEntries.length !== 1 ? 'ies' : 'y'}</Badge>
          </div>
        </CardHeader>
        <CardContent>
          {filteredEntries.length === 0 ? (
            <div className="text-center py-12"><BookText className="h-12 w-12 text-muted-foreground mx-auto mb-4" /><h3 className="text-lg font-semibold mb-2">No journal entries found</h3><p className="text-muted-foreground mb-4">{searchTerm ? 'Try adjusting your search' : 'Sync with QuickBooks to import your journal entries'}</p>{!searchTerm && <Button onClick={sync} disabled={isSyncing}>{isSyncing ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <RefreshCw className="h-4 w-4 mr-2" />}Sync Entries</Button>}</div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow><TableHead>Doc #</TableHead><TableHead>Date</TableHead><TableHead>Total Amount</TableHead><TableHead>Adjustment</TableHead><TableHead>Note</TableHead></TableRow>
                </TableHeader>
                <TableBody>
                  {filteredEntries.map((entry) => (
                    <TableRow key={entry.journalEntryId}>
                      <TableCell><div className="flex items-center"><FileText className="h-4 w-4 mr-2 text-muted-foreground" /><div className="font-medium">{entry.docNumber || '-'}</div></div></TableCell>
                      <TableCell><div className="flex items-center"><Calendar className="h-4 w-4 mr-1 text-muted-foreground" />{formatDate(entry.txnDate)}</div></TableCell>
                      <TableCell><div className="flex items-center font-medium"><DollarSign className="h-4 w-4 text-muted-foreground" />{formatCurrency(entry.totalAmount)}</div></TableCell>
                      <TableCell><Badge variant={entry.adjustment ? 'default' : 'secondary'}>{entry.adjustment ? 'Yes' : 'No'}</Badge></TableCell>
                      <TableCell><div className="max-w-xs truncate text-muted-foreground">{entry.privateNote || '-'}</div></TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
