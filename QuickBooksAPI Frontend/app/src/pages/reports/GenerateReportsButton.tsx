import { Button } from '@/components/ui/button';
import { useReportMutations } from '@/hooks';
import { RefreshCw, Loader2 } from 'lucide-react';

/**
 * Pulls P&L and Balance Sheet history from QuickBooks. Shared by both report pages because one
 * pull refreshes both report types — there is no per-report sync.
 */
export function GenerateReportsButton() {
  const { isGenerating, generateReports } = useReportMutations();

  return (
    <Button onClick={generateReports} disabled={isGenerating} className="font-semibold">
      {isGenerating ? (
        <>
          <Loader2 className="h-4 w-4 animate-spin" />
          Generating...
        </>
      ) : (
        <>
          <RefreshCw className="h-4 w-4" />
          Generate Reports
        </>
      )}
    </Button>
  );
}
