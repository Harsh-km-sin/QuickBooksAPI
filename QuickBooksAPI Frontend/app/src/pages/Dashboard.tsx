import { useDashboardStats } from '@/hooks/useDashboardStats';
import { useQuickBooks } from '@/hooks/useQuickBooks';
import { useAuth } from '@/context/AuthContext';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Users,
  Package,
  Truck,
  Loader2,
  Link2,
  TrendingUp,
  TrendingDown,
  DollarSign,
} from 'lucide-react';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
} from 'recharts';

const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884D8'];

interface StatCardProps {
  title: string;
  value: string | number;
  description?: string;
  icon: React.ElementType;
  trend?: string;
  trendUp?: boolean;
}

function StatCard({ title, value, description, icon: Icon, trend, trendUp }: StatCardProps) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
        <div className="bg-primary/10 p-2 rounded-md">
          <Icon className="h-4 w-4 text-primary" />
        </div>
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">{value}</div>
        {description && <p className="text-xs text-muted-foreground">{description}</p>}
        {trend && <div className={`flex items-center text-xs mt-1 ${trendUp ? 'text-green-600' : 'text-red-600'}`}>{trendUp ? <TrendingUp className="h-3 w-3 mr-1" /> : <TrendingDown className="h-3 w-3 mr-1" />}{trend}</div>}
      </CardContent>
    </Card>
  );
}

export function Dashboard() {
  const { stats, isLoading } = useDashboardStats();
  const { connect, isConnecting } = useQuickBooks();
  const { user } = useAuth();

  const isConnected = user?.realmIds && user.realmIds.length > 0;

  const entityData = stats ? [
    { name: 'Customers', count: stats.customersCount },
    { name: 'Products', count: stats.productsCount },
    { name: 'Vendors', count: stats.vendorsCount },
    { name: 'Bills', count: stats.billsCount },
    { name: 'Invoices', count: stats.invoicesCount },
  ] : [];

  const financialData = stats ? [
    { name: 'Total Invoices', amount: stats.totalInvoiceAmount },
    { name: 'Total Bills', amount: stats.totalBillAmount },
    { name: 'Outstanding Invoices', amount: stats.outstandingInvoiceBalance },
    { name: 'Outstanding Bills', amount: stats.outstandingBillBalance },
  ] : [];

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
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Dashboard</h1>
          <p className="text-muted-foreground">Overview of your accounting data</p>
        </div>
        {!isConnected && (
          <Button onClick={connect} disabled={isConnecting}>
            {isConnecting ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <Link2 className="h-4 w-4 mr-2" />}
            Connect QuickBooks
          </Button>
        )}
      </div>

      {isConnected ? (
        <div className="flex items-center gap-2">
          <Badge variant="default" className="bg-green-600">Connected to QuickBooks</Badge>
          <span className="text-sm text-muted-foreground">{user?.realmIds.length} compan{user?.realmIds.length !== 1 ? 'ies' : 'y'} linked</span>
        </div>
      ) : (
        <Card className="bg-muted/50 border-dashed">
          <CardContent className="flex flex-col items-center justify-center py-8">
            <Link2 className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-semibold mb-2">Connect QuickBooks</h3>
            <p className="text-sm text-muted-foreground text-center max-w-md mb-4">Link your QuickBooks Online account to sync and manage your accounting data</p>
            <Button onClick={connect} disabled={isConnecting}>
              {isConnecting ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <Link2 className="h-4 w-4 mr-2" />}
              Connect Now
            </Button>
          </CardContent>
        </Card>
      )}

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <StatCard title="Total Customers" value={stats?.customersCount || 0} description="Active customers in your account" icon={Users} />
        <StatCard title="Total Products" value={stats?.productsCount || 0} description="Products and services" icon={Package} />
        <StatCard title="Total Vendors" value={stats?.vendorsCount || 0} description="Suppliers and vendors" icon={Truck} />
        <StatCard title="Outstanding Balance" value={formatCurrency((stats?.outstandingInvoiceBalance || 0) - (stats?.outstandingBillBalance || 0))} description="Net outstanding amount" icon={DollarSign} trend={stats && stats.outstandingInvoiceBalance > stats.outstandingBillBalance ? 'Positive' : 'Negative'} trendUp={stats ? stats.outstandingInvoiceBalance > stats.outstandingBillBalance : false} />
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <Card>
          <CardHeader><CardTitle>Entity Overview</CardTitle><CardDescription>Distribution of entities in your account</CardDescription></CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={250}>
              <BarChart data={entityData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="name" />
                <YAxis />
                <Tooltip />
                <Bar dataKey="count" fill="#0088FE" />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        <Card>
          <CardHeader><CardTitle>Financial Overview</CardTitle><CardDescription>Invoices and bills summary</CardDescription></CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={250}>
              <PieChart>
                <Pie data={financialData} cx="50%" cy="50%" labelLine={false} label={({ name, percent }) => `${name}: ${(percent * 100).toFixed(0)}%`} outerRadius={80} fill="#8884d8" dataKey="amount">
                  {financialData.map((_, index) => <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />)}
                </Pie>
                <Tooltip formatter={(value: number) => formatCurrency(value)} />
              </PieChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card><CardHeader className="pb-2"><CardTitle className="text-sm font-medium">Total Invoices</CardTitle></CardHeader><CardContent><div className="text-2xl font-bold">{stats?.invoicesCount || 0}</div><p className="text-xs text-muted-foreground">{formatCurrency(stats?.totalInvoiceAmount || 0)} total value</p></CardContent></Card>
        <Card><CardHeader className="pb-2"><CardTitle className="text-sm font-medium">Total Bills</CardTitle></CardHeader><CardContent><div className="text-2xl font-bold">{stats?.billsCount || 0}</div><p className="text-xs text-muted-foreground">{formatCurrency(stats?.totalBillAmount || 0)} total value</p></CardContent></Card>
        <Card><CardHeader className="pb-2"><CardTitle className="text-sm font-medium">Outstanding Invoices</CardTitle></CardHeader><CardContent><div className="text-2xl font-bold text-green-600">{formatCurrency(stats?.outstandingInvoiceBalance || 0)}</div><p className="text-xs text-muted-foreground">Amount owed to you</p></CardContent></Card>
        <Card><CardHeader className="pb-2"><CardTitle className="text-sm font-medium">Outstanding Bills</CardTitle></CardHeader><CardContent><div className="text-2xl font-bold text-red-600">{formatCurrency(stats?.outstandingBillBalance || 0)}</div><p className="text-xs text-muted-foreground">Amount you owe</p></CardContent></Card>
      </div>
    </div>
  );
}
