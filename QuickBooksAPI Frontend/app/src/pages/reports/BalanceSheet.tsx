import { useState } from 'react';
import { useBalanceSheet, useReportPeriods } from '@/hooks';
import { DatePicker } from '@/components/ui/date-picker';
import { Label } from '@/components/ui/label';
import { Card, CardContent } from '@/components/ui/card';
import { Loader2, AlertCircle } from 'lucide-react';
import { ReportTreeView } from './ReportTreeView';
import { GenerateReportsButton } from './GenerateReportsButton';

function today() {
  const now = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}

export function BalanceSheet() {
  // A balance sheet is a snapshot at an instant, so there is a single date here rather than a
  // range — the backend resolves it to the latest stored month-end at or before this date.
  const [asOfDate, setAsOfDate] = useState(today());

  const { report, isLoading, error } = useBalanceSheet({ asOfDate });
  const { periods } = useReportPeriods('BalanceSheet');

  return (
    <div className="flex h-[calc(100vh-7rem)] lg:h-[calc(100vh-8rem)] flex-col space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 shrink-0">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Balance Sheet</h1>
          <p className="text-muted-foreground">
            Assets, liabilities and equity as of a single date
          </p>
        </div>
        <GenerateReportsButton />
      </div>

      <Card className="shrink-0">
        <CardContent className="flex flex-col sm:flex-row sm:items-end gap-4 pt-6">
          <div className="space-y-2">
            <Label htmlFor="asOfDate">As of</Label>
            <DatePicker id="asOfDate" value={asOfDate} onChange={setAsOfDate} />
          </div>
          {periods.length > 0 && (
            <p className="text-xs text-muted-foreground sm:ml-auto sm:pb-2">
              {periods.length} period{periods.length === 1 ? '' : 's'} synced locally
            </p>
          )}
        </CardContent>
      </Card>

      <Card className="flex-1 min-h-0 overflow-hidden">
        <CardContent className="h-full overflow-y-auto pt-6">
          {isLoading && (
            <div className="flex items-center justify-center py-12 text-muted-foreground">
              <Loader2 className="h-5 w-5 animate-spin mr-2" />
              Loading report...
            </div>
          )}

          {!isLoading && error && (
            <div className="flex items-start gap-2 py-12 justify-center text-destructive">
              <AlertCircle className="h-5 w-5 shrink-0 mt-0.5" />
              <div className="text-sm">
                <p className="font-semibold">Could not load the report</p>
                <p className="text-muted-foreground">{error}</p>
              </div>
            </div>
          )}

          {!isLoading && !error && report && (
            <>
              <p className="mb-4 text-xs text-muted-foreground">
                As of {report.rangeEnd.slice(0, 10)} · {report.accountingMethod} basis
              </p>
              {report.rows.length === 0 ? (
                <p className="py-12 text-center text-sm text-muted-foreground">
                  Nothing synced at or before this date yet. Use “Generate Reports” to pull from QuickBooks.
                </p>
              ) : (
                <ReportTreeView rows={report.rows} />
              )}
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
