import { useState } from 'react';
import { useForecast } from '@/hooks/useForecast';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend } from 'recharts';

const CHART_COLORS = ['#0F766E', '#FACC15', '#3B82F6'];

export function Forecast() {
  const { detail, isLoading, error, createForecast, clearDetail } = useForecast();
  const [name, setName] = useState('');
  const [horizonMonths, setHorizonMonths] = useState(12);
  const [revenuePct, setRevenuePct] = useState(100);
  const [expensePct, setExpensePct] = useState(100);

  const formatCurrency = (v: number) => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(v);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const assumptionsJson = JSON.stringify({
      revenueMultiplier: revenuePct / 100,
      expenseMultiplier: expensePct / 100,
    });
    await createForecast({
      name: name || 'Scenario 1',
      horizonMonths,
      assumptionsJson,
    });
  };

  const chartData = detail?.results.map((r) => ({
    period: new Date(r.periodStart).toLocaleDateString('en-US', { month: 'short', year: '2-digit' }),
    revenue: r.revenue,
    expenses: r.expenses,
    cashBalance: r.cashBalance,
  })) ?? [];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Forecast</h1>
        <p className="text-muted-foreground">Run deterministic scenarios on revenue and expenses</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>New scenario</CardTitle>
          <CardDescription>Set name, horizon, and revenue/expense adjustments (100 = no change)</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4 items-end">
            <div className="space-y-2">
              <Label htmlFor="name">Name</Label>
              <Input
                id="name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="e.g. Base case"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="horizon">Horizon (months)</Label>
              <Input
                id="horizon"
                type="number"
                min={1}
                max={60}
                value={horizonMonths}
                onChange={(e) => setHorizonMonths(Number(e.target.value) || 12)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="revenue">Revenue %</Label>
              <Input
                id="revenue"
                type="number"
                min={0}
                max={200}
                value={revenuePct}
                onChange={(e) => setRevenuePct(Number(e.target.value) || 100)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="expense">Expense %</Label>
              <Input
                id="expense"
                type="number"
                min={0}
                max={200}
                value={expensePct}
                onChange={(e) => setExpensePct(Number(e.target.value) || 100)}
              />
            </div>
            <Button type="submit" disabled={isLoading}>
              {isLoading ? 'Running...' : 'Run forecast'}
            </Button>
          </form>
          {error && <p className="text-destructive text-sm mt-2">{error}</p>}
        </CardContent>
      </Card>

      {detail && (
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <div>
              <CardTitle>{detail.scenario.name}</CardTitle>
              <CardDescription>
                Status: {detail.scenario.status} · Horizon: {detail.scenario.horizonMonths} months
              </CardDescription>
            </div>
            <Button variant="outline" size="sm" onClick={clearDetail}>Clear</Button>
          </CardHeader>
          <CardContent>
            {detail.results.length === 0 ? (
              <p className="text-muted-foreground">No results.</p>
            ) : (
              <>
                <div className="mb-4 flex gap-4 text-sm">
                  <span>Final cash: {formatCurrency(detail.results[detail.results.length - 1]?.cashBalance ?? 0)}</span>
                  {detail.results[detail.results.length - 1]?.runwayMonths != null && (
                    <span>Runway: {detail.results[detail.results.length - 1]!.runwayMonths!.toFixed(1)} months</span>
                  )}
                </div>
                <ResponsiveContainer width="100%" height={320}>
                  <LineChart data={chartData} margin={{ left: 12, right: 12 }}>
                    <CartesianGrid strokeDasharray="3 3" vertical={false} />
                    <XAxis dataKey="period" tick={{ fontSize: 12 }} />
                    <YAxis tickFormatter={(v) => formatCurrency(v)} />
                    <Tooltip formatter={(v: number) => formatCurrency(v)} />
                    <Legend />
                    <Line type="monotone" dataKey="revenue" name="Revenue" stroke={CHART_COLORS[0]} dot={false} strokeWidth={2} />
                    <Line type="monotone" dataKey="expenses" name="Expenses" stroke={CHART_COLORS[1]} dot={false} strokeWidth={2} />
                    <Line type="monotone" dataKey="cashBalance" name="Cash balance" stroke={CHART_COLORS[2]} dot={false} strokeWidth={2} />
                  </LineChart>
                </ResponsiveContainer>
              </>
            )}
          </CardContent>
        </Card>
      )}

      {isLoading && !detail && (
        <Card>
          <CardContent className="pt-6">
            <Skeleton className="h-64 w-full" />
          </CardContent>
        </Card>
      )}
    </div>
  );
}
