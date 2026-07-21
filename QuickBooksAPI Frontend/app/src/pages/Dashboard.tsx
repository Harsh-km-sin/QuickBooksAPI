import { useDashboardStats } from '@/hooks/useDashboardStats';
import { useAuth } from '@/features/auth';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';

export function Dashboard() {
  const { stats, isLoading } = useDashboardStats();
  const { user } = useAuth();

  const isConnected = user?.realmIds && user.realmIds.length > 0;

  const formatCurrency = (value: number) => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div><h1 className="text-3xl font-bold tracking-tight">Dashboard</h1><p className="text-muted-foreground">Overview of your accounting data</p></div>
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          {[...Array(4)].map((_, i) => (
            <Card key={i}><CardHeader className="pb-2"><Skeleton className="h-4 w-24" /></CardHeader><CardContent><Skeleton className="h-8 w-16" /></CardContent></Card>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Dashboard</h1>
          <p className="text-muted-foreground">Overview of your accounting data</p>
        </div>
        {isConnected && (
          <Badge variant="outline" className="w-fit bg-primary/5 text-primary border-primary/20 gap-1 px-3 py-1">
            <div className="h-2 w-2 rounded-full bg-primary animate-pulse" />
            Connected to QuickBooks
          </Badge>
        )}
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
