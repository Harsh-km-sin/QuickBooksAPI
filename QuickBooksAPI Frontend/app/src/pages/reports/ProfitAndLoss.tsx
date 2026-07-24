import { useState } from 'react';
import { useProfitAndLoss, useReportPeriods } from '@/hooks';
import { Card, CardContent } from '@/components/ui/card';
import { Loader2, AlertCircle } from 'lucide-react';
import { ReportTreeView } from './ReportTreeView';
import { GenerateReportsButton } from './GenerateReportsButton';
import { ReportPeriodSelector } from '@/components/reports/ReportPeriodSelector';
import type { AccountingMethod } from '@/components/reports/AccountingMethodSwitch';

function defaultRange() {
  const now = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  const start = `${now.getFullYear()}-01-01`;
  const end = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
  return { start, end };
}

export function ProfitAndLoss() {
  const initial = defaultRange();
  const [startDate, setStartDate] = useState(initial.start);
  const [endDate, setEndDate] = useState(initial.end);
  const [accountingMethod, setAccountingMethod] = useState<AccountingMethod>('Accrual');

  const { report, isLoading, error } = useProfitAndLoss({
    startDate,
    endDate,
    accountingMethod,
  });
  const { periods } = useReportPeriods('ProfitAndLoss');

  return (
    <div className="flex h-[calc(100vh-7rem)] lg:h-[calc(100vh-8rem)] flex-col space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 shrink-0">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Profit &amp; Loss</h1>
          <p className="text-muted-foreground">
            Income and expenses over a period, summed from synced monthly data
          </p>
        </div>
        <GenerateReportsButton />
      </div>

      <Card className="shrink-0">
        <CardContent className="flex flex-col sm:flex-row sm:items-end justify-between gap-4">
          <ReportPeriodSelector
            startDate={startDate}
            endDate={endDate}
            onRangeChange={(start, end) => {
              setStartDate(start);
              setEndDate(end);
            }}
            accountingMethod={accountingMethod}
            onAccountingMethodChange={setAccountingMethod}
          />
          {periods.length > 0 && (
            <p className="text-xs text-muted-foreground sm:pb-2">
              {periods.length} period{periods.length === 1 ? '' : 's'} synced locally
            </p>
          )}
        </CardContent>
      </Card>

      <Card className="flex-1 min-h-0 overflow-hidden">
        <CardContent className="h-full overflow-y-auto">
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
              {report.rows.length === 0 ? (
                <>
                  <p className="mb-4 text-xs text-muted-foreground">
                    {report.rangeStart.slice(0, 10)} to {report.rangeEnd.slice(0, 10)} · {report.accountingMethod} basis
                  </p>
                  <p className="py-12 text-center text-sm text-muted-foreground">
                    Nothing synced for this period and accounting method yet. Use “Generate Reports” to pull from QuickBooks.
                  </p>
                </>
              ) : (
                <ReportTreeView rows={report.rows}>
                  <p className="text-xs text-muted-foreground">
                    {report.rangeStart.slice(0, 10)} to {report.rangeEnd.slice(0, 10)} · {report.accountingMethod} basis
                  </p>
                </ReportTreeView>
              )}
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
