import { useState, useEffect } from 'react';
import { useDashboardStats } from '@/hooks/useDashboardStats';
import { useAnalytics } from '@/hooks/useAnalytics';
import { useAuth } from '@/context/AuthContext';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { Link } from 'react-router-dom';
import { analyticsApi } from '@/api/client';
import type { Entity, ConsolidatedPnlRow } from '@/types';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  TrendingUp,
  TrendingDown,
  DollarSign,
  Clock,
  ClipboardCheck,
} from 'lucide-react';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Cell,
  LineChart,
  Line,
} from 'recharts';
import { AlertTriangle } from 'lucide-react';

/* Theme-aligned chart palette: primary (teal), secondary (mint), accent (gold), info, success */
const CHART_COLORS = ['#0F766E', '#5EEAD4', '#FACC15', '#3B82F6', '#10B981'];

interface StatCardProps {
  title: string;
  value: string | number;
  description?: string;
  icon: React.ElementType;
  trend?: string;
  trendUp?: boolean;
  color?: string;
}

function StatCard({ title, value, description, icon: Icon, trend, trendUp, color = 'primary' }: StatCardProps) {
  const topBarColor = color === 'success' ? 'bg-success' : color === 'destructive' ? 'bg-destructive' : 'bg-primary';
  const iconBgColor = color === 'success' ? 'bg-success/10' : color === 'destructive' ? 'bg-destructive/10' : 'bg-primary/10';
  const iconTextColor = color === 'success' ? 'text-success' : color === 'destructive' ? 'text-destructive' : 'text-primary';

  return (
    <Card className="overflow-hidden">
      <div className={`h-1 w-full ${topBarColor}`} />
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
        <div className={`${iconBgColor} p-2 rounded-md`}>
          <Icon className={`h-4 w-4 ${iconTextColor}`} />
        </div>
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">{value}</div>
        {description && <p className="text-xs text-muted-foreground">{description}</p>}
        {trend && (
          <div className={`flex items-center text-xs mt-1 ${trendUp ? 'text-success' : 'text-destructive'}`}>
            {trendUp ? <TrendingUp className="h-3 w-3 mr-1" /> : <TrendingDown className="h-3 w-3 mr-1" />}
            {trend}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

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
    if (viewMode !== 'consolidated' || selectedEntityId == null) {
      setConsolidatedPnl(null);
      return;
    }
    const to = new Date().toISOString().slice(0, 10);
    const from = new Date();
    from.setMonth(from.getMonth() - 12);
    analyticsApi.getConsolidatedPnl(selectedEntityId, from.toISOString().slice(0, 10), to).then((res) => {
      if (res.success && res.data) setConsolidatedPnl(res.data);
      else setConsolidatedPnl([]);
    });
  }, [viewMode, selectedEntityId]);

  const topVendorsData = vendorSpend ? vendorSpend
    .slice(0, 5)
    .map(v => ({ name: v.vendorName, amount: v.totalSpend })) : [];

  const profitabilityData = customerProfitability ? customerProfitability
    .slice(0, 5)
    .map(c => ({ name: c.customerName, margin: c.marginPct })) : [];

  const revenueExpensesData = revenueExpenses?.map(r => ({
    month: new Date(r.monthStart).toLocaleDateString('en-US', { month: 'short', year: '2-digit' }),
    revenue: r.revenue,
    expenses: r.expenses,
  })) ?? [];

  // KPI sparkline data: one row per date with GrossMargin, RevenueGrowth, BurnMultiple
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
  const kpiSparklineSeries = kpiNames.filter(name => kpis?.some(k => k.kpiName === name));

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
        <StatCard 
          title="Cash Runway" 
          value={`${cashRunway?.runwayMonths || 0} Months`} 
          description="Estimated based on current burn" 
          icon={Clock} 
          trend={cashRunway && cashRunway.runwayMonths > 6 ? 'Healthy' : 'Critical'}
          trendUp={cashRunway ? cashRunway.runwayMonths > 6 : false}
          color={cashRunway && cashRunway.runwayMonths > 6 ? 'success' : 'destructive'}
        />
        <StatCard 
          title="Net Cash Position" 
          value={formatCurrency(cashRunway?.currentCash || 0)} 
          description="Liquid assets across bank accounts" 
          icon={DollarSign} 
        />
        <StatCard 
          title="Monthly Burn" 
          value={formatCurrency(cashRunway?.monthlyBurn || 0)} 
          description="Avg. monthly expenses" 
          icon={TrendingDown} 
          color="destructive"
        />
        <StatCard 
          title="Expected Revenue" 
          value={formatCurrency(cashRunway?.expectedRevenue || 0)} 
          description="Avg. monthly income" 
          icon={TrendingUp} 
          color="success"
        />
      </div>

      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-center justify-between gap-2">
            <div>
              <CardTitle>Revenue vs Expenses</CardTitle>
              <CardDescription>
                {viewMode === 'single' ? 'Last 12 months from warehouse' : 'Consolidated P&L by entity'}
              </CardDescription>
            </div>
            <div className="flex items-center gap-2">
              <Button variant={viewMode === 'single' ? 'default' : 'outline'} size="sm" onClick={() => setViewMode('single')}>
                Single company
              </Button>
              <Button variant={viewMode === 'consolidated' ? 'default' : 'outline'} size="sm" onClick={() => setViewMode('consolidated')}>
                Consolidated
              </Button>
              {viewMode === 'consolidated' && entities.length > 0 && (
                <Select value={selectedEntityId != null ? String(selectedEntityId) : ''} onValueChange={(v) => setSelectedEntityId(v ? parseInt(v, 10) : null)}>
                  <SelectTrigger className="w-[180px]">
                    <SelectValue placeholder="Select entity" />
                  </SelectTrigger>
                  <SelectContent>
                    {entities.filter(e => e.isConsolidatedNode).map((e) => (
                      <SelectItem key={e.id} value={String(e.id)}>{e.name}</SelectItem>
                    ))}
                    {entities.filter(e => e.isConsolidatedNode).length === 0 && entities.map((e) => (
                      <SelectItem key={e.id} value={String(e.id)}>{e.name}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {viewMode === 'single' && (
            <ResponsiveContainer width="100%" height={280}>
              <BarChart data={revenueExpensesData} margin={{ left: 12 }}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} />
                <XAxis dataKey="month" tick={{ fontSize: 12 }} />
                <YAxis tickFormatter={(v) => `$${v}`} />
                <Tooltip formatter={(v: number) => formatCurrency(v)} />
                <Bar dataKey="revenue" name="Revenue" fill={CHART_COLORS[0]} radius={[4, 4, 0, 0]} />
                <Bar dataKey="expenses" name="Expenses" fill={CHART_COLORS[2]} radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          )}
          {viewMode === 'consolidated' && (
            <>
              {!selectedEntityId && (
                <p className="text-muted-foreground py-8 text-center">Select an entity above to view consolidated P&L.</p>
              )}
              {selectedEntityId && consolidatedPnl != null && (
                <ResponsiveContainer width="100%" height={280}>
                  <BarChart
                    data={consolidatedPnl.map((r) => ({
                      month: new Date(r.periodStart).toLocaleDateString('en-US', { month: 'short', year: '2-digit' }),
                      revenue: r.revenue,
                      expenses: r.expenses,
                    }))}
                    margin={{ left: 12 }}
                  >
                    <CartesianGrid strokeDasharray="3 3" vertical={false} />
                    <XAxis dataKey="month" tick={{ fontSize: 12 }} />
                    <YAxis tickFormatter={(v) => `$${v}`} />
                    <Tooltip formatter={(v: number) => formatCurrency(v)} />
                    <Bar dataKey="revenue" name="Revenue" fill={CHART_COLORS[0]} radius={[4, 4, 0, 0]} />
                    <Bar dataKey="expenses" name="Expenses" fill={CHART_COLORS[2]} radius={[4, 4, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              )}
            </>
          )}
        </CardContent>
      </Card>

      {closeIssues != null && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center justify-between gap-2">
              <span className="flex items-center gap-2">
                <ClipboardCheck className="h-5 w-5 text-primary" />
                Close & Data Quality
              </span>
              <Button variant="ghost" size="sm" asChild>
                <Link to="/close-assistant">View all</Link>
              </Button>
            </CardTitle>
            <CardDescription>Month-end close blockers and data-quality issues</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="flex items-center gap-2 text-2xl font-bold mb-2">
              <span className={closeIssues.length > 0 ? 'text-destructive' : 'text-success'}>
                {closeIssues.filter(i => i.severity === 'High').length} High
              </span>
              <span className="text-muted-foreground">/</span>
              <span>{closeIssues.filter(i => i.severity === 'Medium').length} Medium</span>
            </div>
            <ul className="space-y-2 max-h-48 overflow-y-auto">
              {closeIssues.slice(0, 5).map((i) => (
                <li key={i.id} className="flex flex-wrap items-start gap-2 rounded border p-2 text-sm">
                  <Badge variant={i.severity === 'High' ? 'destructive' : 'default'}>{i.severity}</Badge>
                  <span className="font-medium text-muted-foreground">{i.issueType}</span>
                  <span className="flex-1 min-w-0">{i.details ?? '—'}</span>
                  <span className="text-xs text-muted-foreground">{formatDate(i.detectedAt)}</span>
                </li>
              ))}
              {closeIssues.length === 0 && (
                <li className="text-sm text-muted-foreground py-2">No open issues.</li>
              )}
            </ul>
          </CardContent>
        </Card>
      )}

      {anomalies && anomalies.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <AlertTriangle className="h-5 w-5 text-destructive" />
              Anomalies
            </CardTitle>
            <CardDescription>Recent flags from anomaly detection (e.g. spend spikes, large transactions, overdue receivables)</CardDescription>
          </CardHeader>
          <CardContent>
            <ul className="space-y-2 max-h-48 overflow-y-auto">
              {anomalies.slice(0, 10).map((a) => (
                <li key={a.id} className="flex flex-wrap items-start gap-2 rounded border p-2 text-sm">
                  <Badge variant={a.severity === 'High' ? 'destructive' : a.severity === 'Medium' ? 'default' : 'secondary'}>
                    {a.severity}
                  </Badge>
                  <span className="font-medium text-muted-foreground">{a.type}</span>
                  <span className="flex-1 min-w-0">{a.details ?? '—'}</span>
                  <span className="text-xs text-muted-foreground">{formatDate(a.detectedAt)}</span>
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>
      )}

      {kpiSparklineData.length > 0 && kpiSparklineSeries.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>KPI trends</CardTitle>
            <CardDescription>Last 6 months (Gross Margin %, Revenue Growth %, Burn Multiple)</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 md:grid-cols-3">
              {kpiSparklineSeries.map((name, i) => (
                <div key={name} className="h-[120px]">
                  <p className="text-xs font-medium text-muted-foreground mb-1">
                    {name === 'GrossMargin' ? 'Gross Margin %' : name === 'RevenueGrowth' ? 'Revenue Growth %' : 'Burn Multiple'}
                  </p>
                  <ResponsiveContainer width="100%" height={100}>
                    <LineChart data={kpiSparklineData}>
                      <XAxis dataKey="date" hide />
                      <YAxis width={28} tick={{ fontSize: 10 }} tickFormatter={v => (name === 'BurnMultiple' ? v : `${v}%`)} />
                      <Tooltip
                        formatter={(v: number) => (name === 'BurnMultiple' ? v.toFixed(2) : `${Number(v).toFixed(1)}%`)}
                        labelFormatter={l => formatDate(l)}
                      />
                      <Line type="monotone" dataKey={name} stroke={CHART_COLORS[i]} dot={false} strokeWidth={2} />
                    </LineChart>
                  </ResponsiveContainer>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      <div className="grid gap-4 md:grid-cols-7">
        <Card className="md:col-span-4">
          <CardHeader>
            <CardTitle>Top Vendors by Spend</CardTitle>
            <CardDescription>Last 30 days</CardDescription>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={topVendorsData} layout="vertical" margin={{ left: 20 }}>
                <CartesianGrid strokeDasharray="3 3" horizontal={true} vertical={false} />
                <XAxis type="number" tickFormatter={(v) => `$${v}`} hide />
                <YAxis dataKey="name" type="category" width={100} tick={{ fontSize: 12 }} />
                <Tooltip formatter={(v: number) => formatCurrency(v)} />
                <Bar dataKey="amount" fill={CHART_COLORS[0]} radius={[0, 4, 4, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        <Card className="md:col-span-3">
          <CardHeader>
            <CardTitle>Customer Profitability</CardTitle>
            <CardDescription>Gross Margin % per top customer</CardDescription>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={profitabilityData}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} />
                <XAxis dataKey="name" tick={{ fontSize: 10 }} />
                <YAxis tickFormatter={(v) => `${v}%`} />
                <Tooltip formatter={(v: number) => [`${v.toFixed(1)}%`, 'Margin']} />
                <Bar dataKey="margin" radius={[4, 4, 0, 0]}>
                  {profitabilityData.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={entry.margin > 40 ? CHART_COLORS[4] : CHART_COLORS[2]} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card><CardHeader className="pb-2"><CardTitle className="text-sm font-medium">Outstanding Invoices</CardTitle></CardHeader><CardContent><div className="text-2xl font-bold text-success">{formatCurrency(stats?.outstandingInvoiceBalance || 0)}</div><p className="text-xs text-muted-foreground">Amount owed to you</p></CardContent></Card>
        <Card><CardHeader className="pb-2"><CardTitle className="text-sm font-medium">Outstanding Bills</CardTitle></CardHeader><CardContent><div className="text-2xl font-bold text-destructive">{formatCurrency(stats?.outstandingBillBalance || 0)}</div><p className="text-xs text-muted-foreground">Amount you owe</p></CardContent></Card>
        <Card><CardHeader className="pb-2"><CardTitle className="text-sm font-medium">Total Vendors</CardTitle></CardHeader><CardContent><div className="text-2xl font-bold">{stats?.vendorsCount || 0}</div><p className="text-xs text-muted-foreground">Active supply chain</p></CardContent></Card>
        <Card><CardHeader className="pb-2"><CardTitle className="text-sm font-medium">Total Customers</CardTitle></CardHeader><CardContent><div className="text-2xl font-bold">{stats?.customersCount || 0}</div><p className="text-xs text-muted-foreground">Active client base</p></CardContent></Card>
      </div>
    </div>
  );
}
