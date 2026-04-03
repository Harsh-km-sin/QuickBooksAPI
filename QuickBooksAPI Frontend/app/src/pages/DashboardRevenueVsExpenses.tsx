import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import type { Entity, ConsolidatedPnlRow } from '@/types';

export interface DashboardRevenueVsExpensesProps {
  chartColors: string[];
  viewMode: 'single' | 'consolidated';
  onViewModeChange: (mode: 'single' | 'consolidated') => void;
  entities: Entity[];
  selectedEntityId: number | null;
  onSelectedEntityIdChange: (id: number | null) => void;
  revenueExpensesData: { month: string; revenue: number; expenses: number }[];
  consolidatedPnl: ConsolidatedPnlRow[] | null;
  formatCurrency: (value: number) => string;
}

export function DashboardRevenueVsExpenses({
  chartColors,
  viewMode,
  onViewModeChange,
  entities,
  selectedEntityId,
  onSelectedEntityIdChange,
  revenueExpensesData,
  consolidatedPnl,
  formatCurrency,
}: DashboardRevenueVsExpensesProps) {
  return (
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
            <Button variant={viewMode === 'single' ? 'default' : 'outline'} size="sm" onClick={() => onViewModeChange('single')}>
              Single company
            </Button>
            <Button variant={viewMode === 'consolidated' ? 'default' : 'outline'} size="sm" onClick={() => onViewModeChange('consolidated')}>
              Consolidated
            </Button>
            {viewMode === 'consolidated' && entities.length > 0 && (
              <Select
                value={selectedEntityId != null ? String(selectedEntityId) : ''}
                onValueChange={(v) => onSelectedEntityIdChange(v ? parseInt(v, 10) : null)}
              >
                <SelectTrigger className="w-[180px]">
                  <SelectValue placeholder="Select entity" />
                </SelectTrigger>
                <SelectContent>
                  {entities.filter((e) => e.isConsolidatedNode).map((e) => (
                    <SelectItem key={e.id} value={String(e.id)}>{e.name}</SelectItem>
                  ))}
                  {entities.filter((e) => e.isConsolidatedNode).length === 0 && entities.map((e) => (
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
              <Bar dataKey="revenue" name="Revenue" fill={chartColors[0]} radius={[4, 4, 0, 0]} />
              <Bar dataKey="expenses" name="Expenses" fill={chartColors[2]} radius={[4, 4, 0, 0]} />
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
                  <Bar dataKey="revenue" name="Revenue" fill={chartColors[0]} radius={[4, 4, 0, 0]} />
                  <Bar dataKey="expenses" name="Expenses" fill={chartColors[2]} radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </>
        )}
      </CardContent>
    </Card>
  );
}
