import { useNavigate } from 'react-router-dom';
import { useChartOfAccountsMutations } from '@/hooks';
import { Button } from '@/components/ui/button';
import { RefreshCw, Loader2, ChevronLeft } from 'lucide-react';
import { ChartOfAccountsTable } from './ChartOfAccountsTable';

export function ChartOfAccounts() {
  const navigate = useNavigate();
  const { isSyncing, sync } = useChartOfAccountsMutations();

  return (
    <div className="flex h-[calc(100vh-3rem)] lg:h-[calc(100vh-4rem)] flex-col space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 shrink-0">
        <div className="flex items-center gap-2">
          <Button variant="ghost" size="icon" onClick={() => navigate('/settings/master-data')}>
            <ChevronLeft className="h-5 w-5" />
          </Button>
          <div><h1 className="text-3xl font-bold tracking-tight">Chart of Accounts</h1><p className="text-muted-foreground">View your chart of accounts</p></div>
        </div>
        <Button variant="outline" onClick={sync} disabled={isSyncing}>{isSyncing ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <RefreshCw className="h-4 w-4 mr-2" />}Sync</Button>
      </div>

      <ChartOfAccountsTable />
    </div>
  );
}
