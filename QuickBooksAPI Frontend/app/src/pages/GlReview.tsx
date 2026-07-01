import { useMemo, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table';
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select';
import {
  ScanSearch, ChevronLeft, ChevronRight, Download, Search, ShieldAlert, FileWarning,
} from 'lucide-react';
import {
  ResponsiveContainer, BarChart, Bar, XAxis, YAxis, Tooltip, CartesianGrid,
} from 'recharts';
import { useDebouncedValue } from '@/hooks';
import {
  useSelectedRun, useGlRunSummary, useGlTransactions, useGlAnomalyBreakdown, useGlPeriodMetrics,
} from '@/hooks';
import { RunSelector, GlUploadDialog, TransactionRiskDrawer, tierStyle, anomalyLabel } from '@/components/glReview';
import { glReviewApi } from '@/api/glReviewApi';
import type { GlTransactionFilter } from '@/types';

const PAGE_SIZE = 25;
const TIERS = ['critical', 'high', 'medium', 'low', 'normal'] as const;
const formatCurrency = (v: number) =>
  new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(v);

export function GlReview() {
  const { selectedRun, selectedRunId, runs, isLoading: runsLoading } = useSelectedRun();

  if (!runsLoading && runs.length === 0) {
    return (
      <div className="space-y-6">
        <Header />
        <Card>
          <CardContent className="flex flex-col items-center justify-center gap-3 py-16 text-center">
            <ScanSearch className="h-12 w-12 text-muted-foreground" />
            <h3 className="text-lg font-semibold">No GL analysis yet</h3>
            <p className="text-muted-foreground max-w-md">
              Upload a general ledger export (CSV or Excel) to detect outliers, duplicates, reversals, split
              transactions and other anomalies across your journal entries.
            </p>
            <GlUploadDialog />
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <Header runId={selectedRunId} />
      {runsLoading || !selectedRun ? (
        <Skeleton className="h-40 w-full" />
      ) : (
        <>
          <Overview runId={selectedRunId!} />
          <Explorer runId={selectedRunId!} />
        </>
      )}
    </div>
  );
}

function Header({ runId }: { runId?: number | null }) {
  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 className="flex items-center gap-2 text-3xl font-bold tracking-tight">
          <ScanSearch className="h-7 w-7 text-primary" /> GL Review
        </h1>
        <p className="text-muted-foreground">AI anomaly detection across your general ledger.</p>
      </div>
      <div className="flex items-center gap-2">
        <RunSelector />
        {runId != null && (
          <Button asChild variant="outline">
            <a href={glReviewApi.exportCsvUrl(runId)} target="_blank" rel="noreferrer">
              <Download className="h-4 w-4 mr-2" /> Export
            </a>
          </Button>
        )}
        <GlUploadDialog />
      </div>
    </div>
  );
}

function Overview({ runId }: { runId: number }) {
  const { data: summary, isLoading } = useGlRunSummary(runId);
  const { data: breakdown } = useGlAnomalyBreakdown(runId);
  const { data: periods } = useGlPeriodMetrics(runId);

  if (isLoading) return <Skeleton className="h-40 w-full" />;
  const run = summary?.run;
  if (!run) return null;

  const stats = [
    { label: 'Entries', value: String(run.totalTransactions ?? 0) },
    { label: 'Flagged', value: String(run.flaggedCount ?? 0), accent: 'text-destructive' },
    { label: 'Critical', value: String(run.criticalCount ?? 0), accent: 'text-destructive' },
    { label: 'Material exposure', value: formatCurrency(Number(run.materialExposure ?? 0)) },
    { label: 'Avg risk', value: run.avgRiskScore != null ? String(Math.round(run.avgRiskScore)) : '—' },
  ];

  const chartData = (periods ?? []).map((p) => ({
    month: p.month,
    flagged: p.flaggedCount,
    avgRisk: Math.round(p.avgRiskScore),
  }));

  return (
    <>
      {run.aiExecutiveSummary && (
        <Card className="border-l-2 border-l-primary">
          <CardContent className="py-4 text-sm">{run.aiExecutiveSummary}</CardContent>
        </Card>
      )}

      <div className="grid gap-4 sm:grid-cols-3 lg:grid-cols-5">
        {stats.map((s) => (
          <Card key={s.label}>
            <CardHeader className="pb-1"><CardTitle className="text-xs font-medium text-muted-foreground">{s.label}</CardTitle></CardHeader>
            <CardContent><div className={`text-2xl font-bold ${s.accent ?? ''}`}>{s.value}</div></CardContent>
          </Card>
        ))}
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader><CardTitle className="text-base">Flagged entries by period</CardTitle></CardHeader>
          <CardContent>
            {chartData.length === 0 ? (
              <p className="text-sm text-muted-foreground">No period data.</p>
            ) : (
              <ResponsiveContainer width="100%" height={220}>
                <BarChart data={chartData}>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                  <XAxis dataKey="month" fontSize={12} />
                  <YAxis fontSize={12} allowDecimals={false} />
                  <Tooltip />
                  <Bar dataKey="flagged" fill="#0F766E" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader><CardTitle className="text-base">Anomaly types</CardTitle></CardHeader>
          <CardContent className="space-y-2">
            {(breakdown ?? []).slice(0, 8).map((a) => (
              <div key={a.anomalyType} className="space-y-1">
                <div className="flex items-center justify-between text-sm">
                  <span className="truncate">{anomalyLabel(a.anomalyType)}</span>
                  <span className="text-muted-foreground tabular-nums">{a.count} ({a.percentage}%)</span>
                </div>
                <div className="h-1.5 w-full rounded-full bg-muted">
                  <div className="h-full rounded-full bg-primary" style={{ width: `${Math.min(100, a.percentage)}%` }} />
                </div>
              </div>
            ))}
            {(breakdown ?? []).length === 0 && <p className="text-sm text-muted-foreground">No anomalies detected.</p>}
          </CardContent>
        </Card>
      </div>
    </>
  );
}

function Explorer({ runId }: { runId: number }) {
  const [page, setPage] = useState(1);
  const [tier, setTier] = useState<string>('all');
  const [search, setSearch] = useState('');
  const [unreviewedOnly, setUnreviewedOnly] = useState(false);
  const [selectedTx, setSelectedTx] = useState<number | null>(null);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const debouncedSearch = useDebouncedValue(search.trim(), 300);

  const filter: GlTransactionFilter = useMemo(
    () => ({
      page,
      pageSize: PAGE_SIZE,
      riskTier: tier === 'all' ? undefined : tier,
      entityName: debouncedSearch || undefined,
      unreviewedOnly: unreviewedOnly || undefined,
    }),
    [page, tier, debouncedSearch, unreviewedOnly],
  );

  const { data, isLoading, isFetching } = useGlTransactions(runId, filter);
  const items = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  const openTx = (id: number) => {
    setSelectedTx(id);
    setDrawerOpen(true);
  };
  const resetPage = (fn: () => void) => { fn(); setPage(1); };

  return (
    <Card>
      <CardHeader>
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
          <CardTitle className="text-base mr-auto flex items-center gap-2">
            <FileWarning className="h-4 w-4" /> Transaction explorer
          </CardTitle>
          <div className="relative max-w-xs flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search by entity…"
              value={search}
              onChange={(e) => resetPage(() => setSearch(e.target.value))}
              className="pl-9"
            />
          </div>
          <Select value={tier} onValueChange={(v) => resetPage(() => setTier(v))}>
            <SelectTrigger className="w-[140px]"><SelectValue /></SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All tiers</SelectItem>
              {TIERS.map((t) => (
                <SelectItem key={t} value={t}>{tierStyle(t).label}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Button
            variant={unreviewedOnly ? 'default' : 'outline'}
            size="sm"
            onClick={() => resetPage(() => setUnreviewedOnly((v) => !v))}
          >
            Unreviewed
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <Skeleton className="h-[360px] w-full" />
        ) : items.length === 0 ? (
          <div className="py-12 text-center text-muted-foreground">No matching entries.</div>
        ) : (
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Date</TableHead>
                  <TableHead>Account</TableHead>
                  <TableHead>Entity</TableHead>
                  <TableHead className="text-right">Amount</TableHead>
                  <TableHead>Risk</TableHead>
                  <TableHead>Flags</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {items.map((tx) => {
                  const s = tierStyle(tx.riskTier);
                  return (
                    <TableRow key={tx.id} className="cursor-pointer" onClick={() => openTx(tx.id)}>
                      <TableCell className="whitespace-nowrap">{new Date(tx.transactionDate).toLocaleDateString()}</TableCell>
                      <TableCell className="max-w-[180px] truncate">{tx.accountName}</TableCell>
                      <TableCell className="max-w-[140px] truncate">{tx.entityName ?? '—'}</TableCell>
                      <TableCell className="text-right tabular-nums">{formatCurrency(Number(tx.amount))}</TableCell>
                      <TableCell>
                        <Badge variant={s.variant} className={s.className}>
                          {tx.riskScore != null && <ShieldAlert className="h-3 w-3" />}
                          {s.label}{tx.riskScore != null ? ` ${tx.riskScore}` : ''}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <div className="flex flex-wrap gap-1">
                          {tx.anomalyFlags.slice(0, 2).map((f) => (
                            <Badge key={f} variant="secondary" className="text-[10px]">{anomalyLabel(f)}</Badge>
                          ))}
                          {tx.anomalyFlags.length > 2 && (
                            <Badge variant="outline" className="text-[10px]">+{tx.anomalyFlags.length - 2}</Badge>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </div>
        )}

        {totalCount > 0 && (
          <div className="flex items-center justify-between border-t px-1 py-3 mt-2">
            <p className="text-sm text-muted-foreground">
              {(page - 1) * PAGE_SIZE + 1}–{Math.min(page * PAGE_SIZE, totalCount)} of {totalCount}
              {isFetching && ' …'}
            </p>
            <div className="flex items-center gap-2">
              <Button variant="outline" size="sm" onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page <= 1}>
                <ChevronLeft className="h-4 w-4 mr-1" /> Prev
              </Button>
              <span className="text-sm text-muted-foreground">Page {page} of {totalPages}</span>
              <Button variant="outline" size="sm" onClick={() => setPage((p) => Math.min(totalPages, p + 1))} disabled={page >= totalPages}>
                Next <ChevronRight className="h-4 w-4 ml-1" />
              </Button>
            </div>
          </div>
        )}
      </CardContent>

      <TransactionRiskDrawer runId={runId} txId={selectedTx} open={drawerOpen} onOpenChange={setDrawerOpen} />
    </Card>
  );
}
