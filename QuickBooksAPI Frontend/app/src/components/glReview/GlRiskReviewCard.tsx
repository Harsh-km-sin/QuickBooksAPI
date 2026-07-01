import { Link } from 'react-router-dom';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { ScanSearch, ArrowRight, ShieldAlert } from 'lucide-react';
import { useSelectedRun, useGlRunSummary, useGlAnomalyBreakdown } from '@/hooks';
import { tierStyle, TIER_ORDER, anomalyLabel } from './riskTier';
import { GlUploadDialog } from './GlUploadDialog';

const formatCurrency = (v: number) =>
  new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(v);

export function GlRiskReviewCard() {
  const { selectedRun, selectedRunId, isLoading: runsLoading, runs } = useSelectedRun();
  const { data: summary, isLoading: summaryLoading } = useGlRunSummary(selectedRunId);
  const { data: breakdown } = useGlAnomalyBreakdown(selectedRunId);

  if (runsLoading) {
    return (
      <Card>
        <CardHeader><Skeleton className="h-5 w-40" /></CardHeader>
        <CardContent><Skeleton className="h-24 w-full" /></CardContent>
      </Card>
    );
  }

  // Empty state — no runs at all.
  if (runs.length === 0) {
    return (
      <Card className="border-l-2 border-l-primary">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <ScanSearch className="h-5 w-5 text-primary" /> GL Risk Review
          </CardTitle>
          <CardDescription>AI anomaly detection across your general ledger entries.</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col items-start gap-3">
          <p className="text-sm text-muted-foreground">
            Upload a general ledger export to scan for outliers, duplicates, reversals and fraud patterns.
          </p>
          <GlUploadDialog />
        </CardContent>
      </Card>
    );
  }

  const run = summary?.run ?? selectedRun;
  const flagged = run?.flaggedCount ?? 0;
  const total = run?.totalTransactions ?? 0;
  const exposure = run?.materialExposure ?? 0;
  const topAnomalies = (breakdown ?? []).slice(0, 4);

  return (
    <Card className="border-l-2 border-l-primary">
      <CardHeader className="flex flex-row items-start justify-between gap-2">
        <div>
          <CardTitle className="flex items-center gap-2 text-base">
            <ScanSearch className="h-5 w-5 text-primary" /> GL Risk Review
          </CardTitle>
          <CardDescription className="truncate max-w-[280px]">
            {run?.fileName ?? 'Latest run'}
          </CardDescription>
        </div>
        <Button asChild variant="ghost" size="sm">
          <Link to="/gl-review">Open <ArrowRight className="h-4 w-4 ml-1" /></Link>
        </Button>
      </CardHeader>
      <CardContent className="space-y-4">
        {summaryLoading ? (
          <Skeleton className="h-24 w-full" />
        ) : (
          <>
            <div className="grid grid-cols-3 gap-3">
              <div>
                <div className="text-2xl font-bold text-destructive flex items-center gap-1">
                  <ShieldAlert className="h-5 w-5" />{flagged}
                </div>
                <p className="text-xs text-muted-foreground">Flagged of {total}</p>
              </div>
              <div>
                <div className="text-2xl font-bold">{formatCurrency(Number(exposure))}</div>
                <p className="text-xs text-muted-foreground">Material exposure</p>
              </div>
              <div>
                <div className="text-2xl font-bold">{run?.avgRiskScore != null ? Math.round(run.avgRiskScore) : '—'}</div>
                <p className="text-xs text-muted-foreground">Avg risk score</p>
              </div>
            </div>

            <div className="flex flex-wrap gap-1.5">
              {TIER_ORDER.map((tier) => {
                const count = summary?.riskDistribution?.[tier] ?? summary?.riskDistribution?.[
                  tier.charAt(0).toUpperCase() + tier.slice(1)
                ] ?? 0;
                if (!count) return null;
                const s = tierStyle(tier);
                return (
                  <Badge key={tier} variant={s.variant} className={s.className}>
                    {s.label}: {count}
                  </Badge>
                );
              })}
            </div>

            {topAnomalies.length > 0 && (
              <div className="space-y-1.5">
                <p className="text-xs font-medium text-muted-foreground">Top anomaly types</p>
                {topAnomalies.map((a) => (
                  <div key={a.anomalyType} className="flex items-center justify-between text-sm">
                    <span className="truncate">{anomalyLabel(a.anomalyType)}</span>
                    <span className="text-muted-foreground tabular-nums">{a.count}</span>
                  </div>
                ))}
              </div>
            )}
          </>
        )}
      </CardContent>
    </Card>
  );
}
