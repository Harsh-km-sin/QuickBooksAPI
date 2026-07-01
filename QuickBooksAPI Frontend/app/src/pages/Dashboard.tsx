import { useState, useEffect } from 'react';
import { useDashboardStats } from '@/hooks/useDashboardStats';
import { useAnalytics } from '@/hooks/useAnalytics';
import { useAuth } from '@/features/auth';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { analyticsApi } from '@/api/analyticsApi';
import type { Entity, ConsolidatedPnlRow } from '@/types';
import {
  TrendingUp,
  TrendingDown,
  DollarSign,
  Clock,
} from 'lucide-react';
import { DashboardStatCard } from './DashboardStatCard';
import { DashboardRevenueVsExpenses } from './DashboardRevenueVsExpenses';
import { DashboardInsights } from './DashboardInsights';
import { DashboardVendorCharts } from './DashboardVendorCharts';
import { GlRiskReviewCard } from '@/components/glReview';

const CHART_COLORS = ['#0F766E', '#5EEAD4', '#FACC15', '#3B82F6', '#10B981'];

export function Dashboard() {
  const { stats, isLoading: statsLoading } = useDashboardStats();
  const { cashRunway, vendorSpend, customerProfitability, revenueExpenses, anomalies, kpis, closeIssues, isLoading: analyticsLoading } = useAnalytics();
  const { user } = useAuth();
  const [viewMode, setViewMode] = useState<'single' | 'consolidated'>('single');
  const [entities, setEntities] = useState<Entity[]>([]);
  const [selectedEntityId, setSelectedEntityId] = useState<number | null>(null);
  const [consolidatedPnl, setConsolidatedPnl] = useState<ConsolidatedPnlRow[] | null>(null);

  const isConnected = user?.realmIds && user.realmIds.length > 0;
  const isLoading = statsLoading || analyticsLoading;

  useEffect(() => {
    if (viewMode !== 'consolidated') return;
    analyticsApi.getEntities().then((res) => {
      if (res.success && res.data) setEntities(res.data);
      else setEntities([]);
    });
  }, [viewMode]);

  useEffect(() => {
    if (viewMode !== 'consolidated' || selectedEntityId == null) return;
    const to = new Date().toISOString().slice(0, 10);
    const from = new Date();
    from.setMonth(from.getMonth() - 12);
    analyticsApi.getConsolidatedPnl(selectedEntityId, from.toISOString().slice(0, 10), to).then((res) => {
      if (res.success && res.data) setConsolidatedPnl(res.data);
      else setConsolidatedPnl([]);
    });
  }, [viewMode, selectedEntityId]);

  const topVendorsData = vendorSpend
    ? vendorSpend
      .slice(0, 5)
      .map((v) => ({ name: v.vendorName, amount: v.totalSpend }))
    : [];

  const profitabilityData = customerProfitability
    ? customerProfitability
      .slice(0, 5)
      .map((c) => ({ name: c.customerName, margin: c.marginPct }))
    : [];

  const revenueExpensesData = revenueExpenses?.map((r) => ({
    month: new Date(r.monthStart).toLocaleDateString('en-US', { month: 'short', year: '2-digit' }),
    revenue: r.revenue,
    expenses: r.expenses,
  })) ?? [];

  const kpiNames = ['GrossMargin', 'RevenueGrowth', 'BurnMultiple'] as const;
  const kpiSparklineData = (() => {
    if (!kpis?.length) return [] as { date: string; GrossMargin?: number; RevenueGrowth?: number; BurnMultiple?: number }[];
    const byDate = new Map<string, { date: string; GrossMargin?: number; RevenueGrowth?: number; BurnMultiple?: number }>();
    for (const k of kpis) {
      const d = k.snapshotDate.slice(0, 10);
      if (!byDate.has(d)) byDate.set(d, { date: d });
      const row = byDate.get(d)!;
      if (k.kpiName === 'GrossMargin') row.GrossMargin = k.kpiValue;
      else if (k.kpiName === 'RevenueGrowth') row.RevenueGrowth = k.kpiValue;
      else if (k.kpiName === 'BurnMultiple') row.BurnMultiple = k.kpiValue;
    }
    return Array.from(byDate.values()).sort((a, b) => a.date.localeCompare(b.date));
  })();
  const kpiSparklineSeries = kpiNames.filter((name) => kpis?.some((k) => k.kpiName === name));

  const formatCurrency = (value: number) => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);
  const formatDate = (s: string) => new Date(s).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: '2-digit' });

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div><h1 className="text-3xl font-bold tracking-tight">Dashboard</h1><p className="text-muted-foreground">Overview of your accounting data</p></div>
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          {[...Array(4)].map((_, i) => (
            <Card key={i}><CardHeader className="pb-2"><Skeleton className="h-4 w-24" /></CardHeader><CardContent><Skeleton className="h-8 w-16" /></CardContent></Card>
          ))}
        </div>
        <div className="grid gap-4 md:grid-cols-2">
          {[...Array(2)].map((_, i) => (
            <Card key={i} className="h-[300px]"><CardHeader><Skeleton className="h-4 w-32" /></CardHeader><CardContent><Skeleton className="h-[200px] w-full" /></CardContent></Card>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">CFO Intelligence Dashboard</h1>
          <p className="text-muted-foreground">Insights and financial health for {user?.name || 'your company'}</p>
        </div>
        {isConnected && (
          <Badge variant="outline" className="w-fit bg-primary/5 text-primary border-primary/20 gap-1 px-3 py-1">
            <div className="h-2 w-2 rounded-full bg-primary animate-pulse" />
            Connected to QuickBooks
          </Badge>
        )}
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <DashboardStatCard
          title="Cash Runway"
          value={`${cashRunway?.runwayMonths || 0} Months`}
          description="Estimated based on current burn"
          icon={Clock}
          trend={cashRunway && cashRunway.runwayMonths > 6 ? 'Healthy' : 'Critical'}
          trendUp={cashRunway ? cashRunway.runwayMonths > 6 : false}
          color={cashRunway && cashRunway.runwayMonths > 6 ? 'success' : 'destructive'}
        />
        <DashboardStatCard
          title="Net Cash Position"
          value={formatCurrency(cashRunway?.currentCash || 0)}
          description="Liquid assets across bank accounts"
          icon={DollarSign}
        />
        <DashboardStatCard
          title="Monthly Burn"
          value={formatCurrency(cashRunway?.monthlyBurn || 0)}
          description="Avg. monthly expenses"
          icon={TrendingDown}
          color="destructive"
        />
        <DashboardStatCard
          title="Expected Revenue"
          value={formatCurrency(cashRunway?.expectedRevenue || 0)}
          description="Avg. monthly income"
          icon={TrendingUp}
          color="success"
        />
      </div>

      <DashboardRevenueVsExpenses
        chartColors={CHART_COLORS}
        viewMode={viewMode}
        onViewModeChange={setViewMode}
        entities={entities}
        selectedEntityId={selectedEntityId}
        onSelectedEntityIdChange={setSelectedEntityId}
        revenueExpensesData={revenueExpensesData}
        consolidatedPnl={consolidatedPnl}
        formatCurrency={formatCurrency}
      />

      <DashboardInsights
        chartColors={CHART_COLORS}
        closeIssues={closeIssues}
        anomalies={anomalies ?? undefined}
        kpiSparklineData={kpiSparklineData}
        kpiSparklineSeries={kpiSparklineSeries}
        formatDate={formatDate}
      />

      <DashboardVendorCharts
        chartColors={CHART_COLORS}
        topVendorsData={topVendorsData}
        profitabilityData={profitabilityData}
        formatCurrency={formatCurrency}
      />

      <GlRiskReviewCard />

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card><CardHeader className="pb-2"><CardTitle className="text-sm font-medium">Outstanding Invoices</CardTitle></CardHeader><CardContent><div className="text-2xl font-bold text-success">{formatCurrency(stats?.outstandingInvoiceBalance || 0)}</div><p className="text-xs text-muted-foreground">Amount owed to you</p></CardContent></Card>
        <Card><CardHeader className="pb-2"><CardTitle className="text-sm font-medium">Outstanding Bills</CardTitle></CardHeader><CardContent><div className="text-2xl font-bold text-destructive">{formatCurrency(stats?.outstandingBillBalance || 0)}</div><p className="text-xs text-muted-foreground">Amount you owe</p></CardContent></Card>
        <Card><CardHeader className="pb-2"><CardTitle className="text-sm font-medium">Total Vendors</CardTitle></CardHeader><CardContent><div className="text-2xl font-bold">{stats?.vendorsCount || 0}</div><p className="text-xs text-muted-foreground">Active supply chain</p></CardContent></Card>
        <Card><CardHeader className="pb-2"><CardTitle className="text-sm font-medium">Total Customers</CardTitle></CardHeader><CardContent><div className="text-2xl font-bold">{stats?.customersCount || 0}</div><p className="text-xs text-muted-foreground">Active client base</p></CardContent></Card>
      </div>
    </div>
  );
}
