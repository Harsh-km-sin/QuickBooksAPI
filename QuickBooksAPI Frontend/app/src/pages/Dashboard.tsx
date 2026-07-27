import { useDashboardStats } from '@/hooks/useDashboardStats';
import { useAuth } from '@/features/auth';
import { useConnectedCompanies } from '@/features/company';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import {
  TrendingUp,
  TrendingDown,
  Wallet,
  CalendarDays,
  FileDown,
  Landmark,
  Scale,
  PieChart,
  Users,
  Building2,
  Package,
  FileText,
  Receipt,
  Info,
} from 'lucide-react';
import { toast } from 'sonner';

export function Dashboard() {
  const { stats, isLoading } = useDashboardStats();
  const { currentRealmId } = useAuth();
  const { companies } = useConnectedCompanies({ silent: true });
  const currentCompany = companies.find((c) => c.qboRealmId === currentRealmId);

  const formatCurrency = (value: number | null | undefined) => {
    if (value === null || value === undefined) return '$0.00';
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);
  };

  const formatDate = (dateStr: string | null | undefined) => {
    if (!dateStr) return '';
    try {
      return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
    } catch {
      return dateStr;
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-6 max-w-[1440px] mx-auto p-4 sm:p-6">
        <div>
          <Skeleton className="h-9 w-64" />
          <Skeleton className="h-5 w-80 mt-2" />
        </div>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {[...Array(6)].map((_, i) => (
            <Card key={i}>
              <CardContent className="pt-6">
                <Skeleton className="h-24 w-full" />
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  const hasReportData = stats && (stats.totalIncome !== null || stats.totalAssets !== null);

  return (
    <div className="space-y-6 max-w-[1440px] mx-auto p-4 sm:p-6">
      {/* Header */}
      <div className="flex flex-col gap-4 md:flex-row md:justify-between md:items-end">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-foreground">Practice Overview</h1>
          <p className="text-base text-muted-foreground mt-1">
            Real-time financial summary for{' '}
            <span className="font-bold text-primary">{currentCompany?.companyName || 'your business'}</span>.
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => toast.info('Custom date ranges are not available yet')}>
            <CalendarDays className="h-4 w-4 mr-1.5" />
            Last 30 Days
          </Button>
          <Button variant="outline" onClick={() => toast.info('Exporting reports is not available yet')}>
            <FileDown className="h-4 w-4 mr-1.5" />
            Export PDF
          </Button>
        </div>
      </div>

      {!hasReportData && (
        <div className="flex items-center gap-3 p-4 bg-primary/10 border border-primary/20 rounded-xl text-primary text-sm">
          <Info className="h-5 w-5 shrink-0" />
          <span>
            Financial statements have not been synced yet. Visit the Reports page to fetch Profit &amp; Loss and Balance Sheet data.
          </span>
        </div>
      )}

      {/* Row 1 — Profit & Loss Financial KPIs */}
      <div>
        <h2 className="text-xs uppercase tracking-wider text-muted-foreground font-semibold mb-3">
          Profit &amp; Loss Overview {stats?.pnlAccountingMethod ? `(${stats.pnlAccountingMethod} Basis)` : ''}
        </h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {/* Total Income */}
          <Card className="shadow-sm hover:shadow-md transition-all border border-border">
            <CardContent className="pt-6">
              <div className="flex justify-between items-start mb-3">
                <p className="text-xs uppercase tracking-wider text-muted-foreground font-semibold">Total Income</p>
                <div className="p-2 bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 rounded-lg">
                  <TrendingUp className="h-5 w-5" />
                </div>
              </div>
              <p className="text-3xl font-bold tracking-tight text-foreground">{formatCurrency(stats?.totalIncome)}</p>
              <p className="mt-2 text-xs text-muted-foreground">
                {stats?.pnlRangeStart && stats?.pnlRangeEnd
                  ? `${formatDate(stats.pnlRangeStart)} – ${formatDate(stats.pnlRangeEnd)}`
                  : 'Current YTD Period'}
              </p>
            </CardContent>
          </Card>

          {/* Total Expenses */}
          <Card className="shadow-sm hover:shadow-md transition-all border border-border">
            <CardContent className="pt-6">
              <div className="flex justify-between items-start mb-3">
                <p className="text-xs uppercase tracking-wider text-muted-foreground font-semibold">Total Expenses</p>
                <div className="p-2 bg-rose-500/10 text-rose-600 dark:text-rose-400 rounded-lg">
                  <TrendingDown className="h-5 w-5" />
                </div>
              </div>
              <p className="text-3xl font-bold tracking-tight text-foreground">{formatCurrency(stats?.totalExpenses)}</p>
              <p className="mt-2 text-xs text-muted-foreground">
                {stats?.pnlRangeStart && stats?.pnlRangeEnd
                  ? `${formatDate(stats.pnlRangeStart)} – ${formatDate(stats.pnlRangeEnd)}`
                  : 'Current YTD Period'}
              </p>
            </CardContent>
          </Card>

          {/* Net Income / Profit */}
          <Card className="shadow-sm hover:shadow-md transition-all border border-border">
            <CardContent className="pt-6">
              <div className="flex justify-between items-start mb-3">
                <p className="text-xs uppercase tracking-wider text-muted-foreground font-semibold">Net Profit</p>
                <div className="p-2 bg-primary/10 text-primary rounded-lg">
                  <Wallet className="h-5 w-5" />
                </div>
              </div>
              <p
                className={`text-3xl font-bold tracking-tight ${
                  (stats?.netIncome ?? 0) >= 0
                    ? 'text-emerald-600 dark:text-emerald-400'
                    : 'text-rose-600 dark:text-rose-400'
                }`}
              >
                {formatCurrency(stats?.netIncome)}
              </p>
              <p className="mt-2 text-xs text-muted-foreground">Net earnings after expenses</p>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Row 2 — Balance Sheet Overview */}
      <div>
        <h2 className="text-xs uppercase tracking-wider text-muted-foreground font-semibold mb-3">
          Balance Sheet Position {stats?.bsAsOfDate ? `(As of ${formatDate(stats.bsAsOfDate)})` : ''}
        </h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {/* Total Assets */}
          <Card className="shadow-sm hover:shadow-md transition-all border border-border">
            <CardContent className="pt-6">
              <div className="flex justify-between items-start mb-3">
                <p className="text-xs uppercase tracking-wider text-muted-foreground font-semibold">Total Assets</p>
                <div className="p-2 bg-blue-500/10 text-blue-600 dark:text-blue-400 rounded-lg">
                  <Landmark className="h-5 w-5" />
                </div>
              </div>
              <p className="text-3xl font-bold tracking-tight text-foreground">{formatCurrency(stats?.totalAssets)}</p>
              <p className="mt-2 text-xs text-muted-foreground">Total cash, accounts receivable &amp; property</p>
            </CardContent>
          </Card>

          {/* Total Liabilities */}
          <Card className="shadow-sm hover:shadow-md transition-all border border-border">
            <CardContent className="pt-6">
              <div className="flex justify-between items-start mb-3">
                <p className="text-xs uppercase tracking-wider text-muted-foreground font-semibold">Total Liabilities</p>
                <div className="p-2 bg-amber-500/10 text-amber-600 dark:text-amber-400 rounded-lg">
                  <Scale className="h-5 w-5" />
                </div>
              </div>
              <p className="text-3xl font-bold tracking-tight text-foreground">{formatCurrency(stats?.totalLiabilities)}</p>
              <p className="mt-2 text-xs text-muted-foreground">Accounts payable &amp; outstanding debts</p>
            </CardContent>
          </Card>

          {/* Total Equity */}
          <Card className="shadow-sm hover:shadow-md transition-all border border-border">
            <CardContent className="pt-6">
              <div className="flex justify-between items-start mb-3">
                <p className="text-xs uppercase tracking-wider text-muted-foreground font-semibold">Total Equity</p>
                <div className="p-2 bg-indigo-500/10 text-indigo-600 dark:text-indigo-400 rounded-lg">
                  <PieChart className="h-5 w-5" />
                </div>
              </div>
              <p className="text-3xl font-bold tracking-tight text-foreground">{formatCurrency(stats?.totalEquity)}</p>
              <p className="mt-2 text-xs text-muted-foreground">Owner's equity &amp; retained earnings</p>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Row 3 — Entity Counts Summary Strip */}
      <Card className="shadow-sm border border-border">
        <CardContent className="py-4 px-6">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div className="flex items-center gap-2 text-sm font-semibold text-foreground">
              <span>Entity Directory Summary</span>
            </div>
            <div className="flex flex-wrap items-center gap-6 text-sm text-muted-foreground">
              <div className="flex items-center gap-2">
                <Users className="h-4 w-4 text-primary" />
                <span className="font-bold text-foreground">{stats?.customersCount ?? 0}</span> Customers
              </div>
              <div className="flex items-center gap-2">
                <Building2 className="h-4 w-4 text-primary" />
                <span className="font-bold text-foreground">{stats?.vendorsCount ?? 0}</span> Vendors
              </div>
              <div className="flex items-center gap-2">
                <Package className="h-4 w-4 text-primary" />
                <span className="font-bold text-foreground">{stats?.productsCount ?? 0}</span> Products
              </div>
              <div className="flex items-center gap-2">
                <FileText className="h-4 w-4 text-primary" />
                <span className="font-bold text-foreground">{stats?.invoicesCount ?? 0}</span> Invoices
              </div>
              <div className="flex items-center gap-2">
                <Receipt className="h-4 w-4 text-primary" />
                <span className="font-bold text-foreground">{stats?.billsCount ?? 0}</span> Bills
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
