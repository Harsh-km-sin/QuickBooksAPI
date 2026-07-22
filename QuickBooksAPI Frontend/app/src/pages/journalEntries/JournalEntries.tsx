import { useJournalEntryMutations } from '@/hooks';
import { Button } from '@/components/ui/button';
import { RefreshCw, Loader2 } from 'lucide-react';
import { JournalEntryTable } from './JournalEntryTable';

export function JournalEntries() {
  const { isSyncing, sync } = useJournalEntryMutations();

  return (
    <div className="flex h-[calc(100vh-7rem)] lg:h-[calc(100vh-8rem)] flex-col space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 shrink-0">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Journal Entries</h1>
          <p className="text-muted-foreground">View your journal entries</p>
        </div>
        <Button variant="outline" onClick={sync} disabled={isSyncing}>{isSyncing ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <RefreshCw className="h-4 w-4 mr-2" />}Sync</Button>
      </div>

      <JournalEntryTable />
    </div>
  );
}
