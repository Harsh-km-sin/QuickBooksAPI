import { useDashboardStats } from '@/hooks/useDashboardStats';
import { useAuth } from '@/features/auth';
import { useConnectedCompanies } from '@/features/company';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { Progress } from '@/components/ui/progress';
import { TrendingUp, TrendingDown, Wallet, CalendarDays, FileDown, BarChart3, Plus } from 'lucide-react';
import { toast } from 'sonner';

export function Dashboard() {
  const { stats, isLoading } = useDashboardStats();
  const { currentRealmId } = useAuth();
  const { companies } = useConnectedCompanies({ silent: true });
  const currentCompany = companies.find((c) => c.qboRealmId === currentRealmId);

  const formatCurrency = (value: number) =>
    new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);

  const receivablesProgress =
    stats && stats.totalInvoiceAmount > 0
      ? Math.round((stats.outstandingInvoiceBalance / stats.totalInvoiceAmount) * 100)
      : 0;
  const payablesProgress =
    stats && stats.totalBillAmount > 0
      ? Math.round((stats.outstandingBillBalance / stats.totalBillAmount) * 100)
      : 0;

  if (isLoading) {
    return (
      <div className="space-y-6 max-w-[1440px] mx-auto">
        <div>
          <Skeleton className="h-9 w-64" />
          <Skeleton className="h-5 w-80 mt-2" />
        </div>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {[...Array(3)].map((_, i) => (
            <Card key={i}>
              <CardContent>
                <Skeleton className="h-28 w-full" />
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6 max-w-[1440px] mx-auto">
      <div className="flex flex-col gap-4 md:flex-row md:justify-between md:items-end">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-foreground">Practice Overview</h1>
          <p className="text-base text-muted-foreground">
            Real-time financial pulse for{' '}
            <span className="font-bold text-primary">{currentCompany?.companyName || 'your business'}</span>.
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => toast.info('Custom date ranges are not available yet')}>
            <CalendarDays className="h-4 w-4" />
            Last 30 Days
          </Button>
          <Button variant="outline" onClick={() => toast.info('Exporting reports is not available yet')}>
            <FileDown className="h-4 w-4" />
            Export PDF
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <Card className="shadow-sm hover:shadow-md transition-shadow">
          <CardContent>
            <div className="flex justify-between items-start mb-4">
              <p className="text-xs uppercase tracking-wider text-muted-foreground font-semibold">Total Receivables</p>
              <TrendingUp className="h-5 w-5 text-primary" />
            </div>
            <p className="text-3xl font-bold text-foreground">{formatCurrency(stats?.outstandingInvoiceBalance || 0)}</p>
            <Progress value={receivablesProgress} className="mt-6 h-1.5" />
            <p className="mt-2 text-sm text-muted-foreground">{stats?.invoicesCount ?? 0} invoices total</p>
          </CardContent>
        </Card>

        <Card className="shadow-sm hover:shadow-md transition-shadow">
          <CardContent>
            <div className="flex justify-between items-start mb-4">
              <p className="text-xs uppercase tracking-wider text-muted-foreground font-semibold">Total Payables</p>
              <TrendingDown className="h-5 w-5 text-destructive" />
            </div>
            <p className="text-3xl font-bold text-foreground">{formatCurrency(stats?.outstandingBillBalance || 0)}</p>
            <Progress value={payablesProgress} className="mt-6 h-1.5" />
            <p className="mt-2 text-sm text-muted-foreground">{stats?.billsCount ?? 0} bills total</p>
          </CardContent>
        </Card>

        <Card className="shadow-sm hover:shadow-md transition-shadow">
          <CardContent>
            <div className="flex justify-between items-start mb-4">
              <p className="text-xs uppercase tracking-wider text-muted-foreground font-semibold">Net Profit</p>
              <Wallet className="h-5 w-5 text-muted-foreground" />
            </div>
            <p className="text-3xl font-bold text-muted-foreground">Coming soon</p>
            <p className="mt-6 text-sm text-muted-foreground">Profit &amp; loss calculation isn't available yet.</p>
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="bg-card border border-border rounded-xl shadow-sm overflow-hidden">
          <div className="p-6 border-b border-border">
            <h3 className="text-lg font-semibold text-foreground">Total Invoices</h3>
            <p className="text-sm text-muted-foreground">Monthly billing trends</p>
          </div>
          <div className="h-64 flex flex-col items-center justify-center gap-2 text-muted-foreground">
            <BarChart3 className="h-8 w-8 opacity-40" />
            <p className="text-sm">Trend chart coming soon</p>
          </div>
        </div>

        <div className="bg-card border border-border rounded-xl shadow-sm overflow-hidden">
          <div className="p-6 border-b border-border">
            <h3 className="text-lg font-semibold text-foreground">Total Bills</h3>
            <p className="text-sm text-muted-foreground">Outgoing cash flow analysis</p>
          </div>
          <div className="h-64 flex flex-col items-center justify-center gap-2 text-muted-foreground">
            <BarChart3 className="h-8 w-8 opacity-40" />
            <p className="text-sm">Trend chart coming soon</p>
          </div>
        </div>
      </div>

      <div className="bg-card border border-border rounded-xl shadow-sm overflow-hidden">
        <div className="p-6 border-b border-border">
          <h3 className="text-lg font-semibold text-foreground">Recent Transactions</h3>
        </div>
        <div className="py-16 flex flex-col items-center justify-center gap-2 text-muted-foreground">
          <Wallet className="h-8 w-8 opacity-40" />
          <p className="text-sm">Transaction history will appear here once available.</p>
        </div>
      </div>

      <button
        type="button"
        className="fixed bottom-8 right-8 w-14 h-14 bg-primary text-primary-foreground rounded-full shadow-lg flex items-center justify-center hover:scale-110 active:scale-95 transition-all z-40"
        onClick={() => toast.info('Quick transaction entry is not available yet')}
      >
        <Plus className="h-6 w-6" />
      </button>
    </div>
  );
}
